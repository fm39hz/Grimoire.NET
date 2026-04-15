namespace Grimoire.Domain.Exception;

using Exception = System.Exception;

public class UnsupportedOperationException : Exception {
	public UnsupportedOperationException() {
	}

	public UnsupportedOperationException(string message)
		: base(message) {
	}

	public UnsupportedOperationException(string message, Exception inner)
		: base(message, inner) {
	}
}
