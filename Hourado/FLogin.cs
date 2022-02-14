using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace Hourado
{
    public partial class FLogin : Form
    {

        // -------------------- Initilizations --------------------

        private SqlConnection conn = FMainForm.conn;

        public string username = "";



        // --------------------  FLogin --------------------

        public FLogin()
        {
            InitializeComponent();
        }

        private void FLogin_Load(object sender, EventArgs e)
        {
            try
            {
                txtUsername_L.GotFocus += txtUsername_L_GotFocus;
                txtPassword.GotFocus += txtPassword_GotFocus;
            }
            catch (Exception)
            {
                MessageBox.Show("تنظیمات صفحه ورود ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                Application.Exit();
            }
        }

        private void FLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception)
            {

            }
        }



        // -------------------- Functions --------------------

        public string Cal_MD5(string str)
        {
            try
            {
                MD5 md5 = MD5.Create();

                byte[] input = Encoding.ASCII.GetBytes(str);
                byte[] hash_code = md5.ComputeHash(input);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hash_code.Length; i++)
                    sb.Append(hash_code[i].ToString("X2"));

                return sb.ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("هش رمز عبور ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }



        // -------------------- Main Codes --------------------

        private void txtUsername_L_GotFocus(object sender, EventArgs e)
        {
            try
            {
                txtUsername_L.SelectAll();
            }
            catch (Exception)
            {

            }
        }

        private void txtPassword_GotFocus(object sender, EventArgs e)
        {
            try
            {
                txtPassword.SelectAll();
            }
            catch (Exception)
            {

            }
        }


        // -----> GroupBox Login

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsername_L.Text == "")
                txtUsername_L.Focus();
            else if (txtPassword.Text == "")
                txtPassword.Focus();
            else
            {
                try
                {
                    string str_userPass = Cal_MD5(txtPassword.Text);
                    object userPass;

                    if (str_userPass != null)
                        userPass = str_userPass;
                    else
                        userPass = DBNull.Value;

                    string str_getUsername = "SELECT [username] " +
                                             "FROM [tblUser] " +
                                             "WHERE [username] = @username " +
                                             "   AND [user_pass] = @user_pass";

                    SqlCommand cmd_getUsername = new SqlCommand(str_getUsername, conn);
                    cmd_getUsername.Parameters.Add("@username", SqlDbType.NVarChar).Value = txtUsername_L.Text;
                    cmd_getUsername.Parameters.Add("@user_pass", SqlDbType.NVarChar).Value = userPass;

                    SqlDataAdapter adp_getUsername = new SqlDataAdapter(cmd_getUsername);
                    DataTable dt_getUsername = new DataTable();
                    adp_getUsername.Fill(dt_getUsername);

                    if (dt_getUsername.Rows.Count == 0)
                        MessageBox.Show("نام کاربری یا رمز عبور اشتباه است", "خطا");
                    else
                    {
                        if (txtUsername_L.Text == "Admin")
                            username = "";
                        else
                            username = "Hourado";

                        Hide();
                    }

                    txtPassword.ResetText();

                    txtUsername_L.Focus();
                }
                catch (Exception)
                {
                    MessageBox.Show("ورود ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                }
            }
        }

    }
}
