namespace VetAuthMaui;

using System.Text.Json.Serialization;

public class PetResult
{
	[JsonPropertyName("pets")]
	public List<Pet> Pets { get; set; } = new List<Pet>();
}

public class Pet
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("type")] public string Type { get; set; } = "";
	[JsonPropertyName("age")] public string Age { get; set; } = "";
	[JsonPropertyName("weight")] public string Weight { get; set; } = "";
	[JsonPropertyName("last_vac_data")] public string LastVacData { get; set; } = "";

	public string PickerText
	{
		get
		{
			if (Id == 0)
				return "Новый питомец";
			if (string.IsNullOrWhiteSpace(Type))
				return Name;
			return $"{Name} - {Type}";
		}
	}

	public string TypeText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Type))
				return "Вид не указан";

			return Type;
		}
	}

	public string AgeWeightText
	{
		get
		{
			var text = "";
			if (!string.IsNullOrWhiteSpace(Age))
				text = $"Возраст: {Age}";
			if (!string.IsNullOrWhiteSpace(Weight))
			{
				if (string.IsNullOrWhiteSpace(text))
					text = $"Вес: {Weight} кг";
				else
					text += $" · Вес: {Weight} кг";
			}

			if (string.IsNullOrWhiteSpace(text))
				return "Возраст и вес не указаны";

			return text;
		}
	}

	public string VaccinationText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(LastVacData))
				return "Прививка не указана";
			if (DateTime.TryParse(LastVacData, out var date))
				return $"Последняя прививка: {date:dd.MM.yyyy}";
			return $"Последняя прививка: {LastVacData}";
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
	[JsonPropertyName("last_vac_data")] public string LastVacData { get; set; }

	public AddPetData(string phone, string name, string type, string age, string weight, string lastVacData)
	{
		Phone = phone;
		Name = name;
		Type = type;
		Age = age;
		Weight = weight;
		LastVacData = lastVacData;
	}
}

public class UpdatePetData
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("phone")] public string Phone { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; }
	[JsonPropertyName("type")] public string Type { get; set; }
	[JsonPropertyName("age")] public string Age { get; set; }
	[JsonPropertyName("weight")] public string Weight { get; set; }
	[JsonPropertyName("last_vac_data")] public string LastVacData { get; set; }

	public UpdatePetData(int id, string phone, string name, string type, string age, string weight, string lastVacData)
	{
		Id = id;
		Phone = phone;
		Name = name;
		Type = type;
		Age = age;
		Weight = weight;
		LastVacData = lastVacData;
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
