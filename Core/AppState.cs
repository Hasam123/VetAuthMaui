namespace VetAuthMaui;

// данные приложения
public static class State
{
	public static bool IsAdminMode { get; set; }
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
		IsAdminMode = false;
		ClientId = 0;
		ClientName = "";
		ClientPhone = "";
		SelectedPet = null;
		CurrentMedicalRecord = null;
	}
}














