using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IPLocator;

public partial class MainPage : ContentPage
{
    private bool showCredential = false;

    private string currentIP;
    private string CurrentIP
    {
        get => currentIP;
        set
        {
            var ipChanged = currentIP != value && !string.IsNullOrEmpty(value);
            currentIP = value;

            if (ipChanged)
            {
                Task.Run(SendIP);
            }
        }
    }

	public MainPage()
	{
		InitializeComponent();
        UpdateCredential();
        Connectivity.ConnectivityChanged += async (o, e) => await GetIP();
        Task.Run(async () =>
        {
            while (true)
            {
                await GetIP();
                await Task.Delay(new TimeSpan(0, 15, 0));
            }
        });
    }

    private void OnCredentialButtonClicked(object sender, EventArgs e)
    {
        showCredential = !showCredential;
        UpdateCredential();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        await SendIP();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
	{
		await GetIP();
    }

    private void UpdateCredential()
    {
        credentialStack.IsVisible = showCredential;
        credentialButton.Text = showCredential ? "Save" : "Change";
    }

	private async Task GetIP()
    {
		try
        {
            // Get IP
            var ipList = GetIP(NetworkInterfaceType.Wireless80211);
            var ip = ipList.FirstOrDefault();

            // Show IP to UI
            ipLabel.Text = CurrentIP = ip;
        }
		catch (Exception ex)
        {
            await HandleException(ex);
        }
    }

    private async Task SendIP()
    {
        try
        {
            // Check username and password for publishing
            if (string.IsNullOrEmpty(usernameEntry.Text) || string.IsNullOrEmpty(passwordEntry.Text) || string.IsNullOrEmpty(CurrentIP))
                return;

            // Publish IP address
            var message = new MailMessage();
            var smtp = new SmtpClient();
            message.From = new MailAddress(usernameEntry.Text);
            message.To.Add(new MailAddress(usernameEntry.Text));
            message.Subject = "[IP] New address";
            message.Body = $"The new IP address is {CurrentIP}.";
            smtp.Port = 587;
            smtp.Host = "smtp.gmail.com";
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(usernameEntry.Text, passwordEntry.Text);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }

	private List<string> GetIP(NetworkInterfaceType networkInterface) => NetworkInterface.GetAllNetworkInterfaces()
		.Where(i => i.NetworkInterfaceType == networkInterface)
		.SelectMany(i => i.GetIPProperties().UnicastAddresses)
		.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
		.Select(x => x.Address.ToString())
		.ToList();

    private async Task HandleException(Exception ex)
    {
        CurrentIP = null;
        await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
    }
}