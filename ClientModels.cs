namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class ClientResult
{
	[JsonPropertyName("client")]
	public ClientInfo Client { get; set; } = new ClientInfo();

	[JsonPropertyName("requests")]
	public List<Appointment> Requests { get; set; } = new List<Appointment>();
}

public class ClientInfo
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("phone")]
	public string Phone { get; set; } = "";
}

public class PetResult
{
	[JsonPropertyName("pets")]
	public List<Pet> Pets { get; set; } = new List<Pet>();
}

public class Pet
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("age")]
	public string Age { get; set; } = "";

	[JsonPropertyName("weight")]
	public string Weight { get; set; } = "";

	[JsonPropertyName("last_vaccination_date")]
	public string LastVaccinationDate { get; set; } = "";

	public string TypeText => string.IsNullOrWhiteSpace(Type) ? "Вид не указан" : Type;

	public string AgeWeightText
	{
		get
		{
			var parts = new List<string>();

			if (!string.IsNullOrWhiteSpace(Age))
				parts.Add($"Возраст: {Age}");
			if (!string.IsNullOrWhiteSpace(Weight))
				parts.Add($"Вес: {Weight}");

			return parts.Count == 0 ? "Возраст и вес не указаны" : string.Join(" · ", parts);
		}
	}

	public string VaccinationText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(LastVaccinationDate))
				return "Прививка не указана";

			if (DateTime.TryParse(LastVaccinationDate, out var date))
				return $"Последняя прививка: {date:dd.MM.yyyy}";

			return $"Последняя прививка: {LastVaccinationDate}";
		}
	}

}

public class AddPetData
{
	[JsonPropertyName("phone")] public string Phone { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; }
	[JsonPropertyName("type")] public string Type { get; set; }
	[JsonPropertyName("age")] public string Age { get; set; }
	[JsonPropertyName("weight")] public string Weight { get; set; }
	[JsonPropertyName("last_vaccination_date")] public string LastVaccinationDate { get; set; }

	public AddPetData(string phone, string name, string type, string age, string weight, string lastVaccinationDate)
	{
		Phone = phone;
		Name = name;
		Type = type;
		Age = age;
		Weight = weight;
		LastVaccinationDate = lastVaccinationDate;
	}
}

public class UpdatePetData : AddPetData
{
	[JsonPropertyName("id")] public int Id { get; set; }

	public UpdatePetData(int id, string phone, string name, string type, string age, string weight, string lastVaccinationDate)
		: base(phone, name, type, age, weight, lastVaccinationDate)
	{
		Id = id;
	}
}

public class DeletePetData
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("phone")] public string Phone { get; set; }

	public DeletePetData(int id, string phone)
	{
		Id = id;
		Phone = phone;
	}
}

public class CancelData
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("phone")]
	public string Phone { get; set; }

	public CancelData(int id, string phone)
	{
		Id = id;
		Phone = phone;
	}
}

public class ApiResult
{
	[JsonPropertyName("success")] public bool Success { get; set; }
	[JsonPropertyName("message")] public string Message { get; set; } = "";
}

public class Appointment
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("phone")]
	public string Phone { get; set; } = "";

	[JsonPropertyName("comment")]
	public string Comment { get; set; } = "";

	[JsonPropertyName("admin_comment")]
	public string AdminComment { get; set; } = "";

	[JsonPropertyName("pet_name")]
	public string PetName { get; set; } = "";

	[JsonPropertyName("pet_type")]
	public string PetType { get; set; } = "";

	[JsonPropertyName("pet_age")]
	public string PetAge { get; set; } = "";

	[JsonPropertyName("service_title")]
	public string ServiceTitle { get; set; } = "";

	[JsonPropertyName("jaloba")]
	public string Jaloba { get; set; } = "";

	[JsonPropertyName("diagnoz")]
	public string Diagnoz { get; set; } = "";

	[JsonPropertyName("obsled_result")]
	public string ObsledResult { get; set; } = "";

	[JsonPropertyName("naz_lech")]
	public string NazLech { get; set; } = "";

	[JsonPropertyName("procedure_done")]
	public string ProcedureDone { get; set; } = "";

	[JsonPropertyName("treatment_notes")]
	public string TreatmentNotes { get; set; } = "";

	[JsonPropertyName("appointment_at")]
	public string AppointmentAt { get; set; } = "";

	[JsonPropertyName("created")]
	public string Created { get; set; } = "";

	[JsonPropertyName("status")]
	public string Status { get; set; } = "";

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
			var age = "";
			if (!string.IsNullOrWhiteSpace(PetAge))
				age = $", {PetAge}";

			if (string.IsNullOrWhiteSpace(PetName))
				return "Питомец не указан";

			return $"Питомец: {PetName}, {PetType}{age}";
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

			return $"Запись: {FormatDate(AppointmentAt)}";
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

	public string MedText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Diagnoz) && string.IsNullOrWhiteSpace(NazLech))
				return "Медицинская запись: нет";

			var parts = new List<string>();

			if (!string.IsNullOrWhiteSpace(Jaloba))
				parts.Add($"Жалоба: {Jaloba}");
			if (!string.IsNullOrWhiteSpace(Diagnoz))
				parts.Add($"Диагноз: {Diagnoz}");
			if (!string.IsNullOrWhiteSpace(ObsledResult))
				parts.Add($"Результат: {ObsledResult}");
			if (!string.IsNullOrWhiteSpace(NazLech))
				parts.Add($"Лечение: {NazLech}");
			if (!string.IsNullOrWhiteSpace(ProcedureDone))
				parts.Add($"Сделано: {ProcedureDone}");
			if (!string.IsNullOrWhiteSpace(TreatmentNotes))
				parts.Add($"Заметки: {TreatmentNotes}");

			return string.Join("\n", parts);
		}
	}

	public string CreatedText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Created))
				return "";

			return $"Создана: {FormatDate(Created)}";
		}
	}

	public bool CanCancel => Status == "new" || Status == "accepted";

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

	private static string FormatDate(string value)
	{
		if (DateTime.TryParse(value, out var date))
			return date.ToString("dd.MM.yyyy, HH:mm");

		return value;
	}
}


