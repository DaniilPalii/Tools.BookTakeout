using System.Net;
using MemoryPack;

namespace BookTakeout.Core.Serialization.Cookies;

public class CookieMemoryPackFormatter : MemoryPackFormatter<Cookie>
{
	public override void Serialize<TBufferWriter>(
		ref MemoryPackWriter<TBufferWriter> writer,
		scoped ref Cookie? value)
	{
		if (value == null)
		{
			writer.WriteNullObjectHeader();

			return;
		}

		writer.WritePackable(
			new CookieMemoryPackable(
				value.Name,
				value.Value,
				value.Path,
				value.Domain,
				value.Secure,
				value.HttpOnly,
				value.Expires,
				value.Version));
	}

	public override void Deserialize(
		ref MemoryPackReader reader,
		scoped ref Cookie? value)
	{
		if (reader.PeekIsNull())
		{
			reader.Advance(1);
			value = null;

			return;
		}

		var read = reader.ReadPackable<CookieMemoryPackable>();

		value = new(read!.Name, read.Value, read.Path, read.Domain)
		{
			Secure = read.Secure,
			HttpOnly = read.HttpOnly,
			Expires = read.Expires,
			Version = read.Version,
		};
	}
}