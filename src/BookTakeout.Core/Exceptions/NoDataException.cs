namespace BookTakeout.Core.Exceptions;

public class NoDataException(string? message)
	: Exception(message);
