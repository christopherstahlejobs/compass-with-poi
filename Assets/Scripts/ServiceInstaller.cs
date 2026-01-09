using UnityEngine;
/// <summary>
/// Author: Christopher Stahle
/// Purpose: Base class for service installers
/// </summary>
public abstract class ServiceInstaller : MonoBehaviour
{
	protected abstract void InstallService();

	private void Awake()
	{
		InstallService();
	}

	public void Init()
	{
		InstallService();
	}
}