using System.Collections.Generic;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Generic service locator for dependency injection
/// </summary>
public static class ServiceLocator
{
	private static readonly Dictionary<System.Type, object> _services = new Dictionary<System.Type, object>();

	/// <summary>
	/// Register a service
	/// </summary>
	public static void Register<T>(T service) where T : class
	{
		_services[typeof(T)] = service;
	}

	/// <summary>
	/// Unregister a service
	/// </summary>
	public static void Unregister<T>() where T : class
	{
		_services.Remove(typeof(T));
	}

	/// <summary>
	/// Get a service
	/// </summary>
	public static T Get<T>() where T : class
	{
		if (_services.TryGetValue(typeof(T), out object service))
		{
			return service as T;
		}
		return null;
	}
}

