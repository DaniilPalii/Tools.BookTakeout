using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
using BookTakeout.Core.Exceptions;
using BookTakeout.Core.Parsing;
using BookTakeout.Core.Values;
using Microsoft.Extensions.Logging;

namespace BookTakeout.Core;

public partial class LitnetHttpClient(
	HttpClient httpClient,
	CookieContainer cookieContainer,
	CookieStorage cookieStorage,
	LitnetBrowserClient litnetBrowserClient,
	ILogger<LitnetHttpClient> logger)
{
	public TimeSpan BetweenRequestsTimeout { get; set; } = TimeSpan.FromSeconds(seconds: 3);

	private readonly HtmlParser htmlParser = new();
	private string csrfToken = string.Empty;

	private const string BaseUrl = "https://litnet.com";
	private const string BookInfoUrlPrefix = "https://litnet.com/book/";
	private const string BookReaderUrlPrefix = "https://litnet.com/reader/";
	private const string GetPageUrl = "https://litnet.com/reader/get-page";

	public static void ConfigureClient(HttpClient httpClient)
	{
		httpClient.Timeout = TimeSpan.FromSeconds(100);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
		httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.8");
	}

	public static HttpMessageHandler CreateHandler(CookieContainer cookieContainer)
	{
		return new SocketsHttpHandler
		{
			CookieContainer = cookieContainer,
			UseCookies = true,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
		};
	}

	public async Task AuthenticateAsync(
		CancellationToken cancellationToken,
		bool forceLogin = false)
	{
		if (!forceLogin
			&& await cookieStorage.LoadCookiesAsync() is { Count: > 0 } cookies)
		{
			LogLoadedCookiesFromStorage(cookies.Count);
		}
		else
		{
			cookies = await litnetBrowserClient.AuthenticateAsync();
			await cookieStorage.SaveCookiesAsync(cookies);
			LogSavedCookiesToStorage();
		}

		var baseUri = new Uri(BaseUrl);

		foreach (var cookie in cookies)
			cookieContainer.Add(baseUri, cookie);

		var verificationHtml = await httpClient.GetStringAsync(BaseUrl, cancellationToken);
		using var parsedVerificationHtml = await htmlParser.ParseDocumentAsync(verificationHtml);
		var csrfTokenMeta = parsedVerificationHtml.QuerySelector("meta[name='csrf-token']");
		csrfToken = csrfTokenMeta?.GetAttribute("content")
			?? throw new NoDataException("CSRF token not found after login");

		if (verificationHtml.Contains("Авторизация") || verificationHtml.Contains("LoginForm"))
			throw new BadAuthorizationException();

		LogAuthenticationSuccessful();
	}

	public async Task<BookInfo> GetBookInfoWebPageAsync(string bookSlug, CancellationToken cancellationToken)
	{
		await Task.Delay(BetweenRequestsTimeout, cancellationToken);
		var bookInfoUrl = BookInfoUrlPrefix + bookSlug;

		var webPageHtml = await httpClient.GetStringAsync(bookInfoUrl, cancellationToken);
		LogBookInfoPageLoaded(bookInfoUrl);

		var bookInfoPage = await BookInfoWebPage.ParseAsync(webPageHtml, htmlParser);
		var coverImage = await DownloadImageAsync(bookInfoPage.CoverSource, "Cover", cancellationToken);

		return new BookInfo(
			bookInfoPage.Title,
			bookInfoPage.Author,
			bookInfoPage.Annotation,
			bookInfoPage.Series,
			coverImage);
	}

	public async Task<ChapterInfo[]> GetBookChaptersAsync(string bookSlug, CancellationToken cancellationToken)
	{
		await Task.Delay(BetweenRequestsTimeout, cancellationToken);
		var bookReaderUrl = BookReaderUrlPrefix + bookSlug;

		var webPageHtml = await httpClient.GetStringAsync(bookReaderUrl, cancellationToken);
		LogBookReaderPageLoaded(bookReaderUrl);

		return await BookReaderWebPage.GetChaptersInfoAsync(webPageHtml, htmlParser);
	}

	public async Task<(string content, bool isPageLast)> GetBookPageContentAsync(
		string bookSlug,
		string chapterId,
		int pageIndex,
		CancellationToken cancellationToken)
	{
		var chapterUrl = $"{BookReaderUrlPrefix}{bookSlug}?c={chapterId}";
		var response = await PostAsync(
			GetPageUrl,
			contentParameters:
			[
				new(key: "chapterId", value: chapterId),
				new(key: "page", value: pageIndex.ToString()),
				new(key: "_csrf", value: csrfToken),
			],
			referer: chapterUrl,
			cancellationToken: cancellationToken);

		var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

		using var jsonDocument = JsonDocument.Parse(responseText);
		var root = jsonDocument.RootElement;

		var status = root.GetProperty("status").GetInt32();

		if (status is not 1)
		{
			var errorData = root.TryGetProperty("data", out var dataProperty) ? dataProperty.GetString() : "Unknown error";
			throw new NoDataException(message: $"Page status is not 1 but {status}. Response: {errorData}");
		}

		var content = root.GetProperty("data").GetString()
			?? throw new NoDataException(message: $"No data found for page {pageIndex}");

		var isLast = root.TryGetProperty("isLastPage", out var isLastString)
			&& isLastString.GetBoolean();

		return (content, isLast);
	}

	public async Task<byte[]> DownloadImageAsync(string imageSource, string imageDescription, CancellationToken cancellationToken)
	{
		if (imageSource.StartsWith("//"))
			imageSource = "https:" + imageSource;

		if (!Uri.TryCreate(imageSource, UriKind.Absolute, out var imageUri)
			|| (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
		{
			LogInvalidCoverImageUrl(imageSource, imageDescription);
		}

		using var imageResponse = await httpClient.GetAsync(imageUri, cancellationToken);

		if (!imageResponse.IsSuccessStatusCode)
			LogFailedToDownloadImage(imageSource, imageDescription, imageResponse.StatusCode);

		return await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
	}

	private async Task<HttpResponseMessage> PostAsync(
		string url,
		IEnumerable<KeyValuePair<string, string>> contentParameters,
		string referer,
		CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		using var requestContent = new FormUrlEncodedContent(contentParameters);

		request.Content = requestContent;
		request.Headers.Add(name: "Origin", value: BaseUrl);
		request.Headers.Referrer = new Uri(referer);
		request.Headers.Add(name: "x-requested-with", value: "XMLHttpRequest");

		if (!string.IsNullOrEmpty(csrfToken))
			request.Headers.Add(name: "x-csrf-token", value: csrfToken);

		await Task.Delay(BetweenRequestsTimeout, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();

		return await httpClient.SendAsync(request, cancellationToken);
	}

	[LoggerMessage(LogLevel.Information, "Loaded {CookiesCount} cookies from storage")]
	partial void LogLoadedCookiesFromStorage(int cookiesCount);

	[LoggerMessage(LogLevel.Information, "Saved cookies to storage")]
	partial void LogSavedCookiesToStorage();

	[LoggerMessage(LogLevel.Information, "Authentication successful")]
	partial void LogAuthenticationSuccessful();

	[LoggerMessage(LogLevel.Information, "Book info page loaded: {Url}")]
	partial void LogBookInfoPageLoaded(string url);

	[LoggerMessage(LogLevel.Information, "Book reader page loaded: {Url}")]
	partial void LogBookReaderPageLoaded(string url);

	[LoggerMessage(LogLevel.Warning, "Image URL is not an absolute HTTP/HTTPS URL: {ImageSource}, {ImageTitle}")]
	partial void LogInvalidCoverImageUrl(string imageSource, string imageTitle);

	[LoggerMessage(LogLevel.Warning, "Failed to download image {ImageSource}, {ImageTitle}. Status code: {ImageResponseStatusCode}")]
	partial void LogFailedToDownloadImage(string imageSource, string imageTitle, HttpStatusCode imageResponseStatusCode);
}