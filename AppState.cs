namespace VetAuthMaui;

// данные приложения
public static class State
{
	public static bool IsAdminMode { get; set; }
	public static int ClientId { get; set; }
	public static string ClientName { get; set; } = "";
	public static string ClientPhone { get; set; } = "";
	public static MedicalRecord CurrentMedicalRecord { get; set; }

	public static bool IsClientLoggedIn => !string.IsNullOrWhiteSpace(ClientPhone);

	public static void SetClient(int id, string name, string phone)
	{
		ClientId = id;
		ClientName = name;
		ClientPhone = phone;
	}

	public static void LogoutClient()
	{
		IsAdminMode = false;
		ClientId = 0;
		ClientName = "";
		ClientPhone = "";
		CurrentMedicalRecord = null;
	}
}














