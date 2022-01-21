using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SteamGuardCodeGenerator
{
    public partial class Form1 : Form
    {
        Dictionary<string, string> sharedSecrets = new Dictionary<string, string>();
        long time;
        public Form1()
        {
            InitializeComponent();
            this.Size = new Size(595, 90);
            updateTime();
        }

        async void updateTime()
        {
            time = await TimeAligner.GetSteamTimeAsync();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public string GenerateSteamGuardCodeForTime(string sharedSecret, long time)
        {
            if (sharedSecret == null || sharedSecret.Length == 0)
            {
                return "";
            }

            string sharedSecretUnescaped = Regex.Unescape(sharedSecret);
            byte[] sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
            byte[] timeArray = new byte[8];

            time /= 30L;

            for (int i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            HMACSHA1 hmacGenerator = new HMACSHA1();
            hmacGenerator.Key = sharedSecretArray;
            byte[] hashedData = hmacGenerator.ComputeHash(timeArray);
            byte[] codeArray = new byte[5];
            try
            {
                byte b = (byte)(hashedData[19] & 0xF);
                int codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

                for (int i = 0; i < 5; ++i)
                {
                    codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                    codePoint /= steamGuardCodeTranslations.Length;
                }
            }
            catch (Exception)
            {
                return null; //Change later, catch-alls are bad!
            }
            return Encoding.UTF8.GetString(codeArray);
        }

        private static byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        bool opened = false;

        private void Button1_Click(object sender, EventArgs e)
        {
            ChangeFormSize();

        }

        void ChangeFormSize()
        {
            if (this.Size.Height > 90)
            {
                opened = true;
            }
            else
            {
                opened = false;
            }

            if (!opened) { this.Size = new Size(595, 665); button1.Text = "hide settings"; return; }
            if (opened) { this.Size = new Size(595, 90); button1.Text = "show settings"; return; }
        }

        void saveSharedSecret()
        {
            string plainText = textBox2.Text;

            if (plainText.Trim().Trim() == "") { MessageBox.Show("shared secret is empty, nothing saved"); return; }

            sharedSecrets = loadSharedSecretsDictionary(plainText);

            string text2save = cbEncryption.Checked ? StringCipher.OpenSSLEncrypt(plainText, textBox1.Text) : plainText;
            System.IO.File.WriteAllText("sgcg.dat", text2save);
        }

        void loadSharedSecret()
        {
            try
            {
                string text = System.IO.File.ReadAllText("sgcg.dat");

                string plainText = cbEncryption.Checked ? StringCipher.OpenSSLDecrypt(text, textBox1.Text) : text;
                sharedSecrets = loadSharedSecretsDictionary(plainText);

                textBox2.Text = plainText;
            }
            catch (Exception ee) { }
        }

        Dictionary<string, string> loadSharedSecretsDictionary(string sharedSecretsText)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string[] lines = sharedSecretsText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                string login = line.Split(' ')[0];
                string loginSecret = line.Split(' ')[1];

                result[login] = loginSecret;
            }

            return result;
        }

        private void BtnLoadSharedSecret_Click(object sender, EventArgs e)
        {
            loadSharedSecret();
        }

        private void BtnSaveSharedSecret_Click(object sender, EventArgs e)
        {
            saveSharedSecret();
        }

        bool encryptionPassChar = true;

        private void Button3_Click(object sender, EventArgs e)
        {
            encryptionPassChar = !encryptionPassChar;
            textBox1.UseSystemPasswordChar = encryptionPassChar;
            textBox2.UseSystemPasswordChar = encryptionPassChar;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (sharedSecrets.Keys.Count == 0) return;

            Form newForm = new Form();
            newForm.Text = "COPY CODES";
            newForm.Size = new Size(320, 400);

            FlowLayoutPanel layout = new FlowLayoutPanel();
            layout.AutoScroll = true;

            foreach (string login in sharedSecrets.Keys)
            {
                Button b = new Button();
                b.Text = login;
                b.Click += B_Click;
                b.Size = new Size(133, 23);
                layout.Controls.Add(b);
            }


            layout.Size = new Size(newForm.Size.Width - 20, newForm.Size.Height - 50);
            newForm.Controls.Add(layout);
            newForm.ShowDialog();
            newForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }

        private void B_Click(object sender, EventArgs e)
        {
            string login = (sender as Button).Text;
            string loginSecret = sharedSecrets[login];
            string CODE = GenerateSteamGuardCodeForTime(loginSecret, time);
            MessageBox.Show(CODE);

            Clipboard.SetText(CODE);
        }

        private void BtnInfo_Click(object sender, EventArgs e)
        {
            Form newForm = new Form();
            newForm.Size = new Size(800, 200);
            TextBox tb = (new TextBox());
            tb.Multiline = true;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.Size = new Size(newForm.Width - 10, newForm.Height -20);
            newForm.Controls.Add(tb);
            newForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            tb.Text = Constants.LICENSE;
            newForm.ShowDialog();
        }

        private void TmrUpdateSteamTime_Tick(object sender, EventArgs e)
        {
            updateTime();
        }
    }
}

public static class APIEndpoints
{
    public const string STEAMAPI_BASE = "https://api.steampowered.com";
    public const string COMMUNITY_BASE = "https://steamcommunity.com";
    public const string MOBILEAUTH_BASE = STEAMAPI_BASE + "/IMobileAuthService/%s/v0001";
    public static string MOBILEAUTH_GETWGTOKEN = MOBILEAUTH_BASE.Replace("%s", "GetWGToken");
    public const string TWO_FACTOR_BASE = STEAMAPI_BASE + "/ITwoFactorService/%s/v0001";
    public static string TWO_FACTOR_TIME_QUERY = TWO_FACTOR_BASE.Replace("%s", "QueryTime");
}

public static class Constants
{
    public const string LICENSE = @"
vk.com/goniss

this app only provides codes to enter steam account. No confirmations here;

if you previously used SDA, you need your login and parameter shared_secret value (from .maFile) without quotes
enter it like:
accountlogin SJHDHJSDH7822HHDSBH
accountlogin2 skfdJADJNSDNjnsDJ243

BE CAREFUL WITH ENCRYPTION. IF YOU FORGET YOUR ENCRYPTION KEY, INFORMATION WILL BE LOST FOREVER.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
";
}

public class Util
{
    public static long GetSystemUnixTime()
    {
        return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }

    public static byte[] HexStringToByteArray(string hex)
    {
        int hexLen = hex.Length;
        byte[] ret = new byte[hexLen / 2];
        for (int i = 0; i < hexLen; i += 2)
        {
            ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return ret;
    }
}
