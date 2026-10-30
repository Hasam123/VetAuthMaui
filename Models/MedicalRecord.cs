namespace VetAuthMaui;

// запись лечения
public class MedicalRecord
{
	public string DataZabol { get; set; } = "";
	public string PredDiag { get; set; } = "";
	public string KlinPrizn { get; set; } = "";
	public string Ishod { get; set; } = "";
	public string Address { get; set; } = "";
	public string PetPol { get; set; } = "";
	public int? PervPriemId { get; set; }

	public int AppointmentId { get; set; }
	public string ClientName { get; set; } = "";
	public string PetName { get; set; } = "";
	public string AppointmentText { get; set; } = "";
	public string Jaloba { get; set; } = "";
	public string Diagnoz { get; set; } = "";
	public string NazLech { get; set; } = "";
	public string ProcedureDone { get; set; } = "";
	public string TreatmentNotes { get; set; } = "";
}













