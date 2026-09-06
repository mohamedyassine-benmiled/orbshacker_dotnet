namespace Orbshacker;

public class OrbshackerException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class NetworkException(string message, Exception? inner = null) : OrbshackerException(message, inner);
public sealed class SteamNotFoundException(string message) : OrbshackerException(message);
public sealed class DatabaseLoadException(string message) : OrbshackerException(message);
