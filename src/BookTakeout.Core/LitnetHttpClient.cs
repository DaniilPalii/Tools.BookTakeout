using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
using BookTakeout.Core.Exceptions;
using BookTakeout.Core.Values;
using BookTakeout.Core.WebPages;
using Microsoft.Extensions.Logging;

namespace BookTakeout.Core;

public partial class LitnetHttpClient(
	HttpClient httpClient,
	CookieContainer cookieContainer,
	CookieStorage cookieStorage,
	LitnetBrowserClient litnetBrowserClient,
	ILogger<LitnetHttpClient> logger)
{
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

	public TimeSpan BetweenRequestsTimeout { get; set; } = TimeSpan.FromSeconds(seconds: 3);

	public async Task<string> AuthenticateAsync(
		CancellationToken cancellationToken,
		bool forceRelogin = false)
	{
		if (!forceRelogin
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

		(var isLoggedIn, csrfToken) = await ScrapeAsync<HomeWebPage>(BaseUrl, cancellationToken);

		if (!isLoggedIn)
			throw new BadAuthorizationException();

		var userName = (await ScrapeAsync<AccountWebPage>(AccountUrl, cancellationToken)).UserName;

		LogAuthenticationSuccessful(userName);

		return userName;
	}

	public async Task<BookInfo> GetBookInfoWebPageAsync(string bookSlug, CancellationToken cancellationToken)
	{
		await Task.Delay(BetweenRequestsTimeout, cancellationToken);

		var bookInfoUrl = BookInfoUrlPrefix + bookSlug;
		var bookInfoPage = await ScrapeAsync<BookInfoWebPage>(bookInfoUrl, cancellationToken);
		LogBookInfoPageLoaded(bookInfoUrl);

		var coverImage = await DownloadImageAsync(bookInfoPage.CoverSource, "Cover", cancellationToken);

		return new(
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
		var chapters = (await ScrapeAsync<BookReaderWebPage>(bookReaderUrl, cancellationToken)).Chapters;
		LogBookReaderPageLoaded(bookReaderUrl);

		return chapters;
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

	private async Task<TPage> ScrapeAsync<TPage>(string url, CancellationToken cancellationToken)
		where TPage : IWebPage<TPage>
	{
		await using var webPageHtmlStream = await httpClient.GetStreamAsync(url, cancellationToken);
		using var htmlDocument = await htmlParser.ParseDocumentAsync(webPageHtmlStream, cancellationToken);

		return TPage.Parse(htmlDocument);
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
		request.Headers.Referrer = new(referer);
		request.Headers.Add(name: "x-requested-with", value: "XMLHttpRequest");

		if (!string.IsNullOrEmpty(csrfToken))
			request.Headers.Add(name: "x-csrf-token", value: csrfToken);

		await Task.Delay(BetweenRequestsTimeout, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();

		return await httpClient.SendAsync(request, cancellationToken);
	}

	[LoggerMessage(LogLevel.Information, "Loaded {CookiesCount} cookies from storage")]
	private partial void LogLoadedCookiesFromStorage(int cookiesCount);

	[LoggerMessage(LogLevel.Information, "Saved cookies to storage")]
	private partial void LogSavedCookiesToStorage();

	[LoggerMessage(LogLevel.Information, "Successfully authenticated as {UserName}")]
	private partial void LogAuthenticationSuccessful(string userName);

	[LoggerMessage(LogLevel.Information, "Book info page loaded: {Url}")]
	private partial void LogBookInfoPageLoaded(string url);

	[LoggerMessage(LogLevel.Information, "Book reader page loaded: {Url}")]
	private partial void LogBookReaderPageLoaded(string url);

	[LoggerMessage(LogLevel.Warning, "Image URL is not an absolute HTTP/HTTPS URL: {ImageSource}, {ImageTitle}")]
	private partial void LogInvalidCoverImageUrl(string imageSource, string imageTitle);

	[LoggerMessage(LogLevel.Warning, "Failed to download image {ImageSource}, {ImageTitle}. Status code: {ImageResponseStatusCode}")]
	private partial void LogFailedToDownloadImage(string imageSource, string imageTitle, HttpStatusCode imageResponseStatusCode);

	private readonly HtmlParser htmlParser = new();
	private string csrfToken = string.Empty;

	private const string BaseUrl = "https://litnet.com";
	private const string BookInfoUrlPrefix = "https://litnet.com/book/";
	private const string BookReaderUrlPrefix = "https://litnet.com/reader/";
	private const string GetPageUrl = "https://litnet.com/reader/get-page";
	private const string AccountUrl = "https://litnet.com/account";
}
