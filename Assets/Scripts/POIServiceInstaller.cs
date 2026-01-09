/// <summary>
/// Author: Christopher Stahle
/// Purpose: Installs POI service in bootstrap scene
/// </summary>
public sealed class POIServiceInstaller : ServiceInstaller
{
	protected override void InstallService()
	{
		POIService poiService = new();
		ServiceLocator.Register<IPOIService>(poiService);
	}
}