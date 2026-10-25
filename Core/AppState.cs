namespace VetAuthMaui;

// данные приложения
public static class State
{
	public static bool IsAdminMode { get; set; }
	public static int AdminId { get; private set; }
	public static string AdminName { get; private set; } = "";

	public static void SetAdmin(int id, string name)
	{
		AdminId = id;
		AdminName = name;
		CurrentMedicalRecord = null;
		IsAdminMode = true;
	}

	public static void LogoutAdmin()
	{
		AdminId = 0;
		AdminName = "";
		IsAdminMode = false;
		CurrentMedicalRecord = null;
	}
	public static int ClientId { get; set; }
	public static string ClientName { get; set; } = "";
	public static string ClientPhone { get; set; } = "";
	public static Pet SelectedPet { get; set; }
	public static MedicalRecord CurrentMedicalRecord { get; set; }

	public static bool IsClientLoggedIn
	{
		get
		{
			if (string.IsNullOrWhiteSpace(ClientPhone))
				return false;

			return true;
		}
	}

	// Устанавливает значение или состояние.
	public static void SetClient(int id, string name, string phone)
	{
		ClientId = id;
		ClientName = name;
		ClientPhone = phone;
	}

	// Очищает данные текущего клиента после выхода из приложения.
	public static void LogoutClient()
	{
		LogoutAdmin();
		ClientId = 0;
		ClientName = "";
		ClientPhone = "";
		SelectedPet = null;
		CurrentMedicalRecord = null;
	}
}














