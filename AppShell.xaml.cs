namespace VetAuthMaui;

// переходы между страницами
public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		// регистрация страниц
		Routing.RegisterRoute("ClientRegister", typeof(ClientRegister));
		Routing.RegisterRoute("AdminLogin", typeof(AdminLogin));
		Routing.RegisterRoute("AdminPage", typeof(AdminPage));
		Routing.RegisterRoute("About", typeof(About));
		Routing.RegisterRoute("HomePage", typeof(HomePage));
		Routing.RegisterRoute("RecordPage", typeof(RecordPage));
		Routing.RegisterRoute("ClientPage", typeof(ClientPage));
		Routing.RegisterRoute("PetsPage", typeof(PetsPage));
		Routing.RegisterRoute("AddPetPage", typeof(AddPetPage));
		Routing.RegisterRoute("EditPetPage", typeof(EditPetPage));
		Routing.RegisterRoute("ContactPage", typeof(ContactPage));
		Routing.RegisterRoute("RequestPage", typeof(RequestPage));
		Routing.RegisterRoute("MedicalRecordPage", typeof(MedicalRecordPage));
		Routing.RegisterRoute("AdminTimePage", typeof(AdminTimePage));
		Routing.RegisterRoute("ServicePage", typeof(ServicePage));
		Routing.RegisterRoute("AddPage", typeof(AddPage));
		Routing.RegisterRoute("EditPage", typeof(EditPage));
	}
}













