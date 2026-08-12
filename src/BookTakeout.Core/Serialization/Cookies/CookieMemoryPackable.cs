using MemoryPack;

namespace BookTakeout.Core.Serialization.Cookies;

[MemoryPackable]
internal partial record CookieMemoryPackable(
	string Name,
	string? Value,
	string? Path,
	string? Domain,
	bool Secure,
	bool HttpOnly,
	DateTime Expires,
	int Version);
