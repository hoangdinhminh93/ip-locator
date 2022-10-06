using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IPLocator;

public partial class MainPage : ContentPage
{
    private bool showCredential = false;

	public MainPage()
	{
		InitializeComponent();
        Connectivity.ConnectivityChanged += (o, e) => GetAndUpdateIP();
        GetAndUpdateIP();
        UpdateCredential();
    }

    private void OnCredentialButtonClicked(object sender, EventArgs e)
    {
        showCredential = !showCredential;
        UpdateCredential();
    }

    private void OnRefreshClicked(object sender, EventArgs e)
	{
		GetAndUpdateIP();
    }

    private void UpdateCredential()
    {
        credentialStack.IsVisible = showCredential;
        credentialButton.Text = showCredential ? "Save" : "Change";
    }

	private async void GetAndUpdateIP()
    {
		try
        {
            // Check internet connectivity
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

			// Get IP
            var ipList = GetIP(NetworkInterfaceType.Wireless80211);
            var ip = ipList.FirstOrDefault();

            // Show IP to UI
			if (ip == null)
            {
                ipLabel.Text = "Unknown";
                return;
            }
            ipLabel.Text = ip;

            // Check username and password for publishing
            if (string.IsNullOrEmpty(usernameEntry.Text) || string.IsNullOrEmpty(passwordEntry.Text))
                return;

            // Generate body
            var body = $"The new IP address is {ip}.\nThe full IP list is:";
            ipList.ForEach(ip => body += $"\n{ip}");

            // Publish IP address
            var message = new MailMessage();
            var smtp = new SmtpClient();
            message.From = new MailAddress(usernameEntry.Text);
            message.To.Add(new MailAddress(usernameEntry.Text));
            message.Subject = "[IP] New address";
            message.Body = body;
            smtp.Port = 587;
            smtp.Host = "smtp.gmail.com"; //for gmail host  
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(usernameEntry.Text, passwordEntry.Text);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
        }
		catch (Exception ex)
        {
			await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
        }
    }

	private List<string> GetIP(NetworkInterfaceType networkInterface) => NetworkInterface.GetAllNetworkInterfaces()
		.Where(i => i.NetworkInterfaceType == networkInterface)
		.SelectMany(i => i.GetIPProperties().UnicastAddresses)
		.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
		.Select(x => x.Address.ToString())
		.ToList();
}