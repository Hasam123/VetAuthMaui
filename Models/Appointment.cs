namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class Appointment
{
	[JsonPropertyName("data_zabol")] public string DataZabol { get; set; } = "";
	[JsonPropertyName("pred_diag")] public string PredDiag { get; set; } = "";
	[JsonPropertyName("klin_prizn")] public string KlinPrizn { get; set; } = "";
	[JsonPropertyName("ishod")] public string Ishod { get; set; } = "";
	[JsonPropertyName("address")] public string Address { get; set; } = "";
	[JsonPropertyName("pet_pol")] public string PetPol { get; set; } = "";
	[JsonPropertyName("perv_priem_id")] public int? PervPriemId { get; set; }

	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("phone")] public string Phone { get; set; } = "";
	[JsonPropertyName("client_comment")] public string ClientComment { get; set; } = "";
	[JsonPropertyName("admin_comment")] public string AdminComment { get; set; } = "";
	[JsonPropertyName("pet_name")] public string PetName { get; set; } = "";
	[JsonPropertyName("pet_type")] public string PetType { get; set; } = "";
	[JsonPropertyName("pet_age")] public string PetAge { get; set; } = "";
	[JsonPropertyName("service_title")] public string ServiceTitle { get; set; } = "";
	[JsonPropertyName("jaloba")] public string Jaloba { get; set; } = "";
	[JsonPropertyName("diagnoz")] public string Diagnoz { get; set; } = "";
	[JsonPropertyName("naz_lech")] public string NazLech { get; set; } = "";
	[JsonPropertyName("procedure_done")] public string ProcedureDone { get; set; } = "";
	[JsonPropertyName("treatment_notes")] public string TreatmentNotes { get; set; } = "";
	[JsonPropertyName("appointment_at")] public string AppointmentAt { get; set; } = "";
	[JsonPropertyName("data_created")] public string DataCreated { get; set; } = "";
	[JsonPropertyName("status")] public string Status { get; set; } = "";

	[JsonPropertyName("doctor_id")] public int? DoctorId { get; set; }
	[JsonPropertyName("doctor_name")] public string DoctorName { get; set; } = "";
	public string DoctorInfo => string.IsNullOrWhiteSpace(DoctorName) ? "Врач не указан" : $"Врач: {DoctorName}";

	public DateTime AppointmentDate
	{
		get
		{
			if (DateTime.TryParse(AppointmentAt, out var date))
				return date;

			return DateTime.MinValue;
		}
	}

	public string StatusText
	{
		get
		{
			if (Status == "new")
				return "Новая";
			if (Status == "accepted")
				return "Принята";
			if (Status == "done")
				return "Выполнена";
			if (Status == "cancelled")
				return "Отменена";

			return Status;
		}
	}

	public string PetInfo
	{
		get
		{
			if (string.IsNullOrWhiteSpace(PetName))
				return "Питомец не указан";

			if (string.IsNullOrWhiteSpace(PetAge))
				return $"Питомец: {PetName}, {PetType}";

			return $"Питомец: {PetName}, {PetType}, {PetAge}";
		}
	}

	public string ServiceInfo
	{
		get
		{
			if (string.IsNullOrWhiteSpace(ServiceTitle))
				return "Услуга не выбрана";

			return $"Услуга: {ServiceTitle}";
		}
	}

	public string TimeText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(AppointmentAt))
				return "Время не выбрано";

			if (AppointmentDate == DateTime.MinValue)
				return AppointmentAt;

			return AppointmentDate.ToString("dd.MM.yyyy, HH:mm");
		}
	}

	public string AdminText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(AdminComment))
				return "Комментарий администратора: нет";

			return $"Комментарий администратора: {AdminComment}";
		}
	}

	public string CreatedText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(DataCreated))
				return "";

			return $"Создана: {FormatDate(DataCreated)}";
		}
	}
	public bool CanCancel
	{
		get
		{
			if (Status == "new" || Status == "accepted")
				return true;

			return false;
		}
	}
	public string Color
	{
		get
		{
			if (Status == "new")
				return "#4AA3D8";
			if (Status == "accepted")
				return "#FF8A5B";
			if (Status == "done")
				return "#30B878";
			if (Status == "cancelled")
				return "#D9534F";

			return "#657084";
		}
	}

	public string MedText
	{
		get
		{
			var text = "";
            if (PervPriemId.HasValue) text += $"Повторный приём, первичный №{PervPriemId}\n";
            if (!string.IsNullOrWhiteSpace(DataZabol)) text += $"Дата заболевания: {FormatDate(DataZabol, false)}\n";
            if (!string.IsNullOrWhiteSpace(PredDiag)) text += $"Предварительный диагноз: {PredDiag}\n";
            if (!string.IsNullOrWhiteSpace(KlinPrizn)) text += $"Осмотр и исследования: {KlinPrizn}\n";
            if (!string.IsNullOrWhiteSpace(Ishod)) text += $"Исход заболевания: {Ishod}\n";
			if (!string.IsNullOrWhiteSpace(Jaloba)) text += $"Жалоба: {Jaloba}\n";
			if (!string.IsNullOrWhiteSpace(Diagnoz)) text += $"Диагноз: {Diagnoz}\n";
			if (!string.IsNullOrWhiteSpace(NazLech)) text += $"Лечение: {NazLech}\n";
			if (!string.IsNullOrWhiteSpace(ProcedureDone)) text += $"Выполненные процедуры: {ProcedureDone}\n";
			if (!string.IsNullOrWhiteSpace(TreatmentNotes)) text += $"Заметки: {TreatmentNotes}";
			if (string.IsNullOrWhiteSpace(text))
				return "Медицинская запись: нет";

			return text.Trim();
		}
	}

	// Преобразует дату в текст для интерфейса.
	private static string FormatDate(string value, bool includeTime = true)
	{
		DateTime date;
		bool converted = DateTime.TryParse(value, out date);

		if (converted)
			return date.ToString(includeTime ? "dd.MM.yyyy, HH:mm" : "dd.MM.yyyy");

		return value;
	}
}

public class CancelData
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("phone")] public string Phone { get; set; }
	public CancelData(int id, string phone)
	{
		Id = id;
		Phone = phone;
	}
}
