using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StandaloneSDKDemo;
using Stimulsoft.Report;

namespace Hourado
{
    public partial class FMainForm : Form
    {

        #region Initializations

        // #SQL

        public static SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["connectionstring"].ToString());



        // #SDK

        public static SDKHelper sdk;
        private DataTable dt_getAthletesActivenessState;
        private bool isAthletesDeactivationDone = false;



        // #Device

        public static string IP = "192.168.1.201", PORT = "4370", COMMKEY = "654321";



        // #Login

        private FLogin fLogin = new FLogin();



        // #Enrollment

        private int maxAthleteID, rotateState = 0, blinkState = 1, blinkCounter = 0;
        private Image redFingerprintImage = Properties.Resources.fingerprint__8_, newImage;
        private Graphics tableLayoutPanelRegisterAthleteDetailGraphics;
        private Pen redPen = new Pen(Color.Tomato, 2);
        private bool enrollmentDone, formLoaded = false;



        // #Registeration

        private string selectedTab = "", LastPage;



        // #Store

        private bool limitRedoing, eventSubscribed = false;
        private int itemSpecificationID;
        private List<int> dgvItemRegisterErrorRows;
        private string itemPrice_old;



        // #StimulSoft

        private StiReport PrintFactor = new StiReport();
        private List<string> sellFactorShoppingBag = new List<string>();



        // #Reports
        private bool firstTime = true;

        #endregion

        #region MainForm

        /****************************** FMainForm ******************************/

        public FMainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                btnHome_Click(sender, e);

                TimeSpan time;

                if (Convert.ToInt32(ConvertDateTime.m2sh(DateTime.Now).Substring(3, 2)) <= 6)
                    time = new TimeSpan(04, 30, 00);
                else
                    time = new TimeSpan(03, 30, 00);

                clockMain.UtcOffset = time;

                conn.Open();
                conn.Close();

                fLogin.ShowDialog();

                if (fLogin.username == "")
                    txtSettingsUserName.ReadOnly = txtSettingsUserOldPassword.Enabled = false;
                else
                {
                    txtSettingsUserName.Text = fLogin.username;
                    fLogin.username = "";
                }

                sdk = new SDKHelper(conn, pcbRegisterFingerImage, dgvMainSideAthletePresence, dgvMainSideSanses, lblMoneyPerSection, btnConnectDevice);
                tableLayoutPanelRegisterAthleteDetailGraphics = tableLayoutRegisterAthleteDetail.CreateGraphics();

                ShrinkDB();

                DeactivateInactiveAthletes();

                if (ConvertDateTime.m2sh(DateTime.Now).Substring(0, 2) == "01")
                    ResetTuitionCredits();

                InitializeDgvStoreShoppingBag();
                InitializeDgvStoreManageItemRegister();
                InitializeDgvMainSideAthletePresence();

                InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                InitializeMainSideDaySans(dgvMainSideSanses);

                lblMoneyPerSection.Text = MoneyPerSection().ToString();

                getCourseCharge(lblRegisterRankCoursePrice, txtRegisterRankCourseSections, lblRegisterRankSectionsPrice);

                if (char.IsDigit(Convert.ToChar(lblRegisterRankSideCredit.Text.Substring(0, 1))))
                    lblRegisterRankRemainSections.Text = (Convert.ToInt32(lblRegisterRankSideCredit.Text) / Convert.ToInt32(lblRegisterRankSectionsPrice.Text)).ToString();
            }
            catch (SqlException)
            {
                MessageBox.Show("ارتباط با دیتابیس مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                Application.Exit();
            }
            catch (Exception)
            {
                MessageBox.Show("اجرای برنامه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                Application.Exit();
            }
        }

        private void FMainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                formLoaded = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void FMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (layoutRegister.Visible && !enrollmentDone && sdk.enrollmentDone)
                {
                    sdk.sta_DisConnect();
                    if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                    {
                        MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                        return;
                    }

                    if (btnRegisterSubmit.Text == "ثبت نهایی")
                    {
                        if (sdk.sta_GetUserInfo(maxAthleteID) == 1 &&
                            sdk.sta_DeleteEnrollData(maxAthleteID, 12) != 1)
                            MessageBox.Show("بروزرسانی دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                    else
                    {
                        if (sdk.sta_GetUserInfo((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value) == 1 &&
                            sdk.sta_DeleteEnrollData((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, 12) != 1)
                            MessageBox.Show("بروزرسانی دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void FMainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                if (formLoaded)
                    ShrinkDB();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------


        private void ResetTuitionCredits()
        {
            try
            {
                string str_getAthletesID = "SELECT [athlete_ID] " +
                                           "FROM [tblAthlete] " +
                                           "WHERE [isactive] = 1";

                SqlCommand cmd_getAthletesID = new SqlCommand(str_getAthletesID, conn);

                SqlDataAdapter adp_getAthletesID = new SqlDataAdapter(cmd_getAthletesID);
                DataTable dt_getAthletesID = new DataTable();
                adp_getAthletesID.Fill(dt_getAthletesID);

                DateTime now = DateTime.Now;

                conn.Open();

                for (int i = 0; i < dt_getAthletesID.Rows.Count; i++)
                {
                    string str_getAthleteCredit = "SELECT [dbo].[GetAthleteCredit](@athlete_ID)";

                    SqlCommand cmd_getAthleteCredit = new SqlCommand(str_getAthleteCredit, conn);
                    cmd_getAthleteCredit.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = (int)dt_getAthletesID.Rows[i][0];

                    SqlDataAdapter adp_getAthleteCredit = new SqlDataAdapter(cmd_getAthleteCredit);
                    DataTable dt_getAthleteCredit = new DataTable();
                    adp_getAthleteCredit.Fill(dt_getAthleteCredit);

                    if ((int)dt_getAthleteCredit.Rows[0][0] > 0)
                    {
                        string str_newAthleteCharge = "INSERT INTO [tblAthleteCharge] " +
                                                      "VALUES (@athlete_ID, 1, @athlete_pay, @charge_date)";

                        SqlCommand cmd_newAthleteCharge = new SqlCommand(str_newAthleteCharge, conn);
                        cmd_newAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = (int)dt_getAthletesID.Rows[i][0];
                        cmd_newAthleteCharge.Parameters.Add("@athlete_pay", SqlDbType.Int).Value = -(int)dt_getAthleteCredit.Rows[0][0];
                        cmd_newAthleteCharge.Parameters.Add("@charge_date", SqlDbType.DateTime2).Value = now;

                        cmd_newAthleteCharge.ExecuteNonQuery();
                    }
                }

                conn.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetTuitionCredits(int athleteID)
        {
            try
            {
                string str_getAthleteCredit = "SELECT [dbo].[GetAthleteCredit](@athlete_ID)";

                SqlCommand cmd_getAthleteCredit = new SqlCommand(str_getAthleteCredit, conn);
                cmd_getAthleteCredit.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                SqlDataAdapter adp_getAthleteCredit = new SqlDataAdapter(cmd_getAthleteCredit);
                DataTable dt_getAthleteCredit = new DataTable();
                adp_getAthleteCredit.Fill(dt_getAthleteCredit);

                if ((int)dt_getAthleteCredit.Rows[0][0] > 0)
                {
                    string str_newAthleteCharge = "INSERT INTO [tblAthleteCharge] " +
                                                  "VALUES (@athlete_ID, 1, @athlete_pay, @charge_date)";

                    SqlCommand cmd_newAthleteCharge = new SqlCommand(str_newAthleteCharge, conn);
                    cmd_newAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;
                    cmd_newAthleteCharge.Parameters.Add("@athlete_pay", SqlDbType.Int).Value = -(int)dt_getAthleteCredit.Rows[0][0];
                    cmd_newAthleteCharge.Parameters.Add("@charge_date", SqlDbType.DateTime2).Value = DateTime.Now;

                    if (conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                        cmd_newAthleteCharge.ExecuteNonQuery();
                        conn.Close();
                    }
                    else
                        cmd_newAthleteCharge.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ShrinkDB()
        {
            try
            {
                string str_shrinkDB = "DBCC SHRINKDATABASE('HouradoDB')";

                SqlCommand cmd_shrinkDB = new SqlCommand(str_shrinkDB, conn);

                conn.Open();
                cmd_shrinkDB.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void DeactivateInactiveAthletes()
        {
            try
            {
                string str_getAthletesActivenessState = "SELECT [tblAthletePresence].[athlete_ID], " +
                                                        "       CASE WHEN DATEADD(MONTH, 3, MAX([presence_date])) < GETDATE() THEN 0 ELSE 1 END AS [activeness] " +
                                                        "FROM [tblAthletePresence] " +
                                                        "INNER JOIN [tblAthlete] ON [tblAthletePresence].[athlete_ID] = [tblAthlete].[athlete_ID] " +
                                                        "WHERE [tblAthlete].[isactive] = 1 " +
                                                        "GROUP BY [tblAthletePresence].[athlete_ID]";

                SqlCommand cmd_getAthletesActivenessState = new SqlCommand(str_getAthletesActivenessState, conn);

                SqlDataAdapter adp_getAthletesActivenessState = new SqlDataAdapter(cmd_getAthletesActivenessState);
                dt_getAthletesActivenessState = new DataTable();
                adp_getAthletesActivenessState.Fill(dt_getAthletesActivenessState);

                if (dt_getAthletesActivenessState.Rows.Count > 0)
                {
                    conn.Open();

                    for (int i = 0; i < dt_getAthletesActivenessState.Rows.Count; i++)
                    {
                        if ((int)dt_getAthletesActivenessState.Rows[i]["activeness"] == 0)
                        {
                            string str_updateAthletesActivenessState = "UPDATE [tblAthlete] " +
                                                                       "SET [isactive] = 0 " +
                                                                       "WHERE [tblAthlete].[athlete_ID] = @athlete_ID";

                            SqlCommand cmd_updateAthletesActivenessState = new SqlCommand(str_updateAthletesActivenessState, conn);
                            cmd_updateAthletesActivenessState.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dt_getAthletesActivenessState.Rows[i]["athlete_ID"];

                            cmd_updateAthletesActivenessState.ExecuteNonQuery();

                            ResetTuitionCredits((int)dt_getAthletesActivenessState.Rows[i]["athlete_ID"]);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        /****************************** General UI ******************************/


        // -------------------- Menu Buttons --------------------


        // -----> Clicks

        private void btnHome_Click(object sender, EventArgs e)
        {
            try
            {
                layoutRegister.Visible = false;
                layoutShop.Visible = false;
                layoutExam.Visible = false;
                layoutSettings.Visible = false;
                layoutReports.Visible = false;
                layoutRegisterRank.Visible = false;
                layoutPays.Visible = false;

                selectedTab = "خانه";
                topButtons_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            try
            {
                layoutRegister.Visible = false;
                layoutShop.Visible = false;
                layoutExam.Visible = false;
                layoutReports.Visible = false;
                layoutRegisterRank.Visible = false;
                layoutPays.Visible = false;

                ResetSettingsPage();

                selectedTab = "تنظیمات";
                topButtons_MouseLeave(sender, e);

                layoutSettings.Visible = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetSettingsPage()
        {
            try
            {
                getExamData(cmbSettingsEditExam);
                getAgeCategoryData(cmbSettingsEditAgeCategory);
                getAgeCategoryData(cmbSettingsSansAgeCategory);
                getCourseDataSettings();

                lblMoneyPerSection.Text = MoneyPerSection().ToString();

                cmbSettingsEditSans.SelectedIndex = -1;
                cmbSettingsEditAgeCategory.SelectedIndex = -1;
                cmbSettingsSansAgeCategory.SelectedIndex = -1;
                cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";
                cmbSettingsSansGender.Text = "انتخاب سالن";

                cmbSettingsEditSans.Text = "سالن انتخاب شود";
                cmbSettingsEditAgeCategory.Text = "انتخاب گروه سنی";
                cmbSettingsEditSansGender.Text = "انتخاب سالن";

                cmbSettingsSansGender.SelectedIndex = -1;
                mtxtSettingsSansStartTime.ResetText();
                mtxtSettingsSansEndTime.ResetText();
                txtSettingsAgeCategoryName.ResetText();
                txtSettingsAgeCategoryAgeFrom.ResetText();
                txtSettingsAgeCategoryAgeTo.ResetText();
                txtSettingsUserOldPassword.ResetText();
                txtSettingsUserNewPassword.ResetText();

                btnSettingsAgeCategoryRemove.Visible = false;
                btnSettingsReturnAgeCategory.Visible = false;
                btnSettingsSansRemove.Visible = false;
                btnSettingsReturnSans.Visible = false;
                cmbSettingsEditSans.Enabled = false;

                btnSettingsAddAgeCategory.Text = "افزودن";
                btnSettingsAddSans.Text = "افزودن";
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegister.Text)
                {
                    layoutShop.Visible = false;
                    layoutExam.Visible = false;
                    layoutSettings.Visible = false;
                    layoutReports.Visible = false;
                    layoutRegisterRank.Visible = false;
                    layoutPays.Visible = false;

                    InitializeRegisterPage();

                    btnRegister.Image = Properties.Resources.add_user_green;
                    lblRegister.ForeColor = Color.Green;

                    selectedTab = lblRegister.Text;
                    topButtons_MouseLeave(sender, e);

                    layoutRegister.Visible = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExam_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblExam.Text)
                {
                    layoutRegister.Visible = false;
                    layoutShop.Visible = false;
                    layoutSettings.Visible = false;
                    layoutReports.Visible = false;
                    layoutRegisterRank.Visible = false;
                    layoutPays.Visible = false;

                    chbExamShowReadyAthletes.Checked = true;

                    FillDgvExamAthletes(dgvExamAthletes);


                    getExamData(cmbExamRank);
                    ResetExamPage();

                    btnExam.Image = Properties.Resources.test_green;
                    lblExam.ForeColor = Color.Green;

                    selectedTab = lblExam.Text;
                    topButtons_MouseLeave(sender, e);

                    layoutExam.Visible = true;

                    btnExamLastExamReturn_Click(sender, e);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnShop_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblShop.Text)
                {
                    layoutRegister.Visible = false;
                    layoutExam.Visible = false;
                    layoutSettings.Visible = false;
                    layoutReports.Visible = false;
                    layoutRegisterRank.Visible = false;
                    layoutPays.Visible = false;

                    btnShop.Image = Properties.Resources.shopping_cart_green;
                    lblShop.ForeColor = Color.Green;

                    selectedTab = lblShop.Text;
                    topButtons_MouseLeave(sender, e);

                    if (tabControlStore.SelectedTab == tabPageStoreSell)
                    {
                        InitializeStorePage();
                        layoutShop.Visible = true;
                    }
                    else
                    {
                        InitializeStoreManagementPage();
                        layoutShop.Visible = true;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegisterRank_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegisterRank.Text)
                {
                    GetAthletesFullName(cmbRegisterRankAthlete);

                    layoutRegister.Visible = false;
                    layoutShop.Visible = false;
                    layoutExam.Visible = false;
                    layoutSettings.Visible = false;
                    layoutReports.Visible = false;
                    layoutPays.Visible = false;

                    ResetRegisterRankPage();

                    getRankData(cmbRegisterRankRank);

                    mtxtRegisterRankDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                    getCourseCharge(lblRegisterRankCoursePrice, txtRegisterRankCourseSections, lblRegisterRankSectionsPrice);

                    btnRegisterRank.Image = Properties.Resources.team_green;
                    lblRegisterRank.ForeColor = Color.Green;

                    selectedTab = lblRegisterRank.Text;
                    topButtons_MouseLeave(sender, e);

                    layoutRegisterRank.Visible = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblReports.Text)
                {
                    layoutRegister.Visible = false;
                    layoutShop.Visible = false;
                    layoutExam.Visible = false;
                    layoutSettings.Visible = false;
                    layoutRegisterRank.Visible = false;
                    layoutPays.Visible = false;

                    tabControlReports_SelectedIndexChanged(sender, e);

                    btnReports.Image = Properties.Resources.report_green;
                    lblReports.ForeColor = Color.Green;

                    selectedTab = lblReports.Text;
                    topButtons_MouseLeave(sender, e);

                    layoutReports.Visible = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnPays_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblPay.Text)
                {
                    LastPage = selectedTab;

                    mtxtPayDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                    layoutReports.Visible = false;
                    layoutRegister.Visible = false;
                    layoutShop.Visible = false;
                    layoutExam.Visible = false;
                    layoutSettings.Visible = false;
                    layoutRegisterRank.Visible = false;

                    GetAthletesFullName(cmbPayAthlete);

                    btnPays.Image = Properties.Resources.money_green;
                    lblPay.ForeColor = Color.Green;

                    selectedTab = lblPay.Text;
                    topButtons_MouseLeave(sender, e);

                    ResetPayPage();

                    layoutPays.Visible = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }


        // -----> Hovers - Leaves - Clicks

        private void topButtons_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnRegisterRank_MouseLeave(sender, e);
                btnRegister_MouseLeave(sender, e);
                btnShop_MouseLeave(sender, e);
                btnExam_MouseLeave(sender, e);
                btnPays_MouseLeave(sender, e);
                btnReports_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegister_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnRegister_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegister_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnRegister_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegister_Click(object sender, EventArgs e)
        {
            try
            {
                btnRegister_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegister_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegister.Text)
                {
                    btnRegister.Image = Properties.Resources.add_user_red;
                    lblRegister.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegister_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegister.Text)
                {
                    btnRegister.Image = Properties.Resources.add_user;
                    lblRegister.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegisterRank_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnRegisterRank_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegisterRank_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnRegisterRank_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegisterRank_Click(object sender, EventArgs e)
        {
            try
            {
                btnRegisterRank_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        void btnRegisterRank_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegisterRank.Text)
                {
                    btnRegisterRank.Image = Properties.Resources.team_red;
                    lblRegisterRank.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegisterRank_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblRegisterRank.Text)
                {
                    btnRegisterRank.Image = Properties.Resources.team;
                    lblRegisterRank.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblShop_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnShop_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblShop_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnShop_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblShop_Click(object sender, EventArgs e)
        {
            try
            {
                btnShop_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnShop_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblShop.Text)
                {
                    btnShop.Image = Properties.Resources.shopping_cart_red;
                    lblShop.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnShop_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblShop.Text)
                {
                    btnShop.Image = Properties.Resources.shopping_cart__3_;
                    lblShop.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblExam_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnExam_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblExam_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnExam_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblExam_Click(object sender, EventArgs e)
        {
            try
            {
                btnExam_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExam_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblExam.Text)
                {
                    btnExam.Image = Properties.Resources.test_red;
                    lblExam.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExam_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblExam.Text)
                {
                    btnExam.Image = Properties.Resources.test;
                    lblExam.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblReports_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnReports_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblReports_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnReports_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblReports_Click(object sender, EventArgs e)
        {
            try
            {
                btnReports_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReports_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblReports.Text)
                {
                    btnReports.Image = Properties.Resources.report_red;
                    lblReports.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReports_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblReports.Text)
                {
                    btnReports.Image = Properties.Resources.report_1_;
                    lblReports.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblPay_MouseHover(object sender, EventArgs e)
        {
            try
            {
                btnPays_MouseHover(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblPay_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnPays_MouseLeave(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }
        private void lblPay_Click(object sender, EventArgs e)
        {
            try
            {
                btnPays_Click(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnPays_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblPay.Text)
                {
                    btnPays.Image = Properties.Resources.money_red;
                    lblPay.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnPays_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (selectedTab != lblPay.Text)
                {
                    btnPays.Image = Properties.Resources.money;
                    lblPay.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Tab Controls --------------------

        private void tabControlStore_Selected(object sender, TabControlEventArgs e)
        {
            try
            {
                if (e.TabPage == tabControlStore.TabPages["tabPageStoreManagement"])
                    InitializeStoreManagementPage();
                else if (e.TabPage == tabControlStore.TabPages["tabPageStoreSell"])
                    InitializeStorePage();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Timers --------------------

        // -----> Functions

        private void BlinkBtnRestartDevice()
        {
            try
            {
                blinkCounter = 0;
                blinkState = 1;
                btnConnectDevice.BackgroundImage = null;

                timerbtnConnectblink.Start();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void FirstRotated()
        {
            try
            {
                newImage = Properties.Resources.reload_1_;
                btnRestartDevice.Image = newImage;
                rotateState++;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void SecondRotated()
        {
            try
            {
                newImage = Properties.Resources.reload_2_;
                btnRestartDevice.Image = newImage;
                rotateState++;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ThirdRotated()
        {
            try
            {
                newImage = Properties.Resources.reload_3_;
                btnRestartDevice.Image = newImage;
                rotateState++;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ForthRotated()
        {
            try
            {
                newImage = Properties.Resources.reload_4_;
                btnRestartDevice.Image = newImage;
                rotateState = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void BlinkOn()
        {
            try
            {
                btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;
                blinkState = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void BlinkOff()
        {
            try
            {
                btnConnectDevice.BackgroundImage = null;
                blinkState = 1;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }


        // -----> Ticks

        private void timerRestartTimeLong_Tick(object sender, EventArgs e)
        {
            try
            {
                timerbtnRestartDevice.Stop();
                FirstRotated();
                timerRestartTimeLong.Stop();
                btnConnectDevice_Click(sender, e);
            }
            catch (Exception)
            {

            }
        }

        private void timerbtnConnectblink_Tick(object sender, EventArgs e)
        {
            try
            {
                if (blinkCounter == 3)
                {
                    timerbtnConnectblink.Stop();
                }
                else
                {
                    if (blinkState == 1)
                        BlinkOn();
                    else
                        BlinkOff();

                    blinkCounter++;
                }
            }
            catch (Exception)
            {

            }
        }

        private void timerbtnRestartDevice_Tick(object sender, EventArgs e)
        {
            try
            {
                switch (rotateState)
                {
                    case 0:
                        FirstRotated();
                        break;
                    case 1:
                        SecondRotated();
                        break;
                    case 2:
                        ThirdRotated();
                        break;
                    case 3:
                        ForthRotated();
                        break;
                    case 4:
                        FirstRotated();
                        break;
                }
            }
            catch (Exception)
            {

            }
        }



        #endregion

        #region MainSide

        public static void InitializeMainSideAthletePresence(DataGridView dgvName, Label lblMoneyPerSession)
        {
            try
            {
                if (dgvName.RowCount > 0)
                    dgvName.Rows.Clear();

                string str_getRecentAthletesPresence = "SELECT TOP (10) [athlete_ID], CASE WHEN [tblAgeCategory].[age_category_name] IS NULL THEN N'ثبت نشده' ELSE [tblAgeCategory].[age_category_name] END AS [ageCategoryName], [tblAthletePresence].[sections_num] " +
                                                       "FROM [tblAthletePresence] " +
                                                       "INNER JOIN [tblSans] ON [tblAthletePresence].[sans_ID] = [tblSans].[sans_ID] " +
                                                       "LEFT JOIN [tblAgeCategory] ON [tblSans].[age_category_ID] = [tblAgeCategory].[age_category_ID] " +
                                                       "ORDER BY [presence_date] DESC";

                SqlCommand cmd_getRecentAthletesPresence = new SqlCommand(str_getRecentAthletesPresence, conn);

                SqlDataAdapter adp_getRecentAthletesPresence = new SqlDataAdapter(cmd_getRecentAthletesPresence);
                DataTable dt_getRecentAthletesPresence = new DataTable();
                adp_getRecentAthletesPresence.Fill(dt_getRecentAthletesPresence);

                if (dt_getRecentAthletesPresence.Rows.Count > 0)
                {
                    object athleteID, athleteName, athleteCredit, athleteSessionsPassed, athleteAgeCategory, athleteSessionsNum;
                    Image athletePic = null;

                    for (int i = 0; i < dt_getRecentAthletesPresence.Rows.Count; i++)
                    {
                        athleteID = dt_getRecentAthletesPresence.Rows[i]["athlete_ID"];
                        athleteAgeCategory = dt_getRecentAthletesPresence.Rows[i]["ageCategoryName"];
                        athleteSessionsNum = dt_getRecentAthletesPresence.Rows[i]["sections_num"];

                        int j = dgvName.Rows.Count;

                        while (j-- > 0)
                        {
                            if ((int)dgvName["athleteID", j].Value == (int)athleteID)
                                break;
                        }

                        if (j == -1)
                        {
                            string str_getMainSideAthleteDetail = "SELECT [tblAthlete].[ismale], [tblName].[name_title] + ' ' + [tblAthlete].[l_name] AS [fullName], " +
                                                                  "       [dbo].[GetAthleteCredit]([tblAthlete].[athlete_ID]) AS [credit], " +
                                                                  "       [dbo].[GetAthleteSessionsPassed]([tblAthlete].[athlete_ID], NULL, NULL) AS [sessionsPassed], " +
                                                                  "       [tblAthletePicture].[picture_data] " +
                                                                  "FROM [tblAthlete] " +
                                                                  "INNER JOIN [tblName] ON [tblAthlete].[name_ID] = [tblName].[name_ID] " +
                                                                  "LEFT JOIN [tblAthletePicture] ON [tblAthlete].[athlete_ID] = [tblAthletePicture].[athlete_ID] " +
                                                                  "WHERE [tblAthlete].[athlete_ID] = @athlete_ID";

                            SqlCommand cmd_getMainSideAthleteDetail = new SqlCommand(str_getMainSideAthleteDetail, conn);
                            cmd_getMainSideAthleteDetail.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                            SqlDataAdapter adp_getMainSideAthleteDetail = new SqlDataAdapter(cmd_getMainSideAthleteDetail);
                            DataTable dt_getMainSideAthleteDetail = new DataTable();
                            adp_getMainSideAthleteDetail.Fill(dt_getMainSideAthleteDetail);

                            if (dt_getMainSideAthleteDetail.Rows.Count > 0)
                            {
                                athleteSessionsPassed = dt_getMainSideAthleteDetail.Rows[0]["sessionsPassed"];

                                if ((int)athleteSessionsPassed == 0)
                                    athleteSessionsPassed = GetEarlierRankSessionsPassed(athleteID);

                                athleteName = dt_getMainSideAthleteDetail.Rows[0]["fullName"];
                                athleteCredit = dt_getMainSideAthleteDetail.Rows[0]["credit"];

                                if (Convert.IsDBNull(dt_getMainSideAthleteDetail.Rows[0]["picture_data"]))
                                {
                                    object isMale = dt_getMainSideAthleteDetail.Rows[0]["ismale"];

                                    if (Convert.IsDBNull(isMale) || Convert.ToBoolean(isMale))
                                        athletePic = Properties.Resources.man__2_;
                                    else
                                        athletePic = Properties.Resources.woman__2_;
                                }
                                else
                                    athletePic = GetImage(dt_getMainSideAthleteDetail.Rows[0]["picture_data"]);

                                dgvName.Rows.Add(dt_getRecentAthletesPresence.Rows.Count - dgvName.RowCount, athleteID, athleteName, athleteCredit, athleteSessionsPassed, athleteAgeCategory, athleteSessionsNum, athletePic);
                            }
                        }
                        else
                        {
                            athleteSessionsPassed = Convert.ToInt32(dgvName["athleteSessionsPassed", j].Value) - Convert.ToInt32(dgvName["athleteSessionsNum", j].Value);

                            if ((int)athleteSessionsPassed == 0)
                                athleteSessionsPassed = GetEarlierRankSessionsPassed(athleteID);

                            athleteName = dgvName["athleteName", j].Value;
                            athleteCredit = Convert.ToInt32(dgvName["athleteCredit", j].Value) + Convert.ToInt32(dgvName["athleteSessionsNum", j].Value) * Convert.ToInt32(lblMoneyPerSession.Text);
                            athletePic = (Image)dgvName["athletePicture", j].Value;

                            dgvName.Rows.Add(dt_getRecentAthletesPresence.Rows.Count - dgvName.RowCount, athleteID, athleteName, athleteCredit, athleteSessionsPassed, athleteAgeCategory, athleteSessionsNum, athletePic);
                        }
                    }

                    SetDgvColumnColor(dgvName, "athleteCredit");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public static void InitializeMainSideDaySans(DataGridView dgvName)
        {
            try
            {
                string str_getMainSideDaySans = "SELECT [سانس] ,[سالن], " +
                                                "       [ساعت شروع] ,[ساعت پایان], " +
                                                "       [افراد حاضر شده در سالن] " +
                                                "FROM [dbo].[GetSanses]()";

                SqlCommand cmd_getMainSideDaySans = new SqlCommand(str_getMainSideDaySans, conn);

                SqlDataAdapter adp_getMainSideDaySans = new SqlDataAdapter(cmd_getMainSideDaySans);
                DataTable dt_getMainSideDaySans = new DataTable();
                adp_getMainSideDaySans.Fill(dt_getMainSideDaySans);

                if (dt_getMainSideDaySans.Rows.Count > 0)
                    dgvName.DataSource = dt_getMainSideDaySans;
                else
                    dgvName.DataSource = null;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------

        private void InitializeDgvMainSideAthletePresence()
        {
            try
            {
                dgvMainSideAthletePresence.Columns.Add("rowID", "شماره");
                dgvMainSideAthletePresence.Columns.Add("athleteID", "کد");
                dgvMainSideAthletePresence.Columns.Add("athleteName", "نام هنرجو");
                dgvMainSideAthletePresence.Columns.Add("athleteCredit", "اعتبار");
                dgvMainSideAthletePresence.Columns.Add("athleteSessionsPassed", "جلسات سپری شده");
                dgvMainSideAthletePresence.Columns.Add("athleteSans", "سانس");
                dgvMainSideAthletePresence.Columns.Add("athleteSessionsNum", "تعداد جلسه");

                DataGridViewImageColumn athletePicture = new DataGridViewImageColumn
                {
                    Name = "athletePicture",
                    HeaderText = "عکس پرسنلی",
                    ImageLayout = DataGridViewImageCellLayout.Stretch
                };
                dgvMainSideAthletePresence.Columns.Add(athletePicture);

                foreach (DataGridViewColumn column in dgvMainSideAthletePresence.Columns)
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;

                dgvMainSideAthletePresence.Columns["rowID"].Visible = false;

                dgvMainSideAthletePresence.Columns["athleteID"].Width = 55;
                dgvMainSideAthletePresence.Columns["athleteName"].Width = 110;
                dgvMainSideAthletePresence.Columns["athleteCredit"].Width = 75;
                dgvMainSideAthletePresence.Columns["athleteSans"].Width = 75;
                dgvMainSideAthletePresence.Columns["athletePicture"].Width = 95;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private static int GetEarlierRankSessionsPassed(object athleteID)
        {
            try
            {
                string str_getMaxAthleteRank = "SELECT COUNT([athlete_ID]) " +
                                               "FROM [tblAthleteRank] " +
                                               "WHERE [athlete_ID] = @athlete_ID";

                SqlCommand cmd_getMaxAthleteRank = new SqlCommand(str_getMaxAthleteRank, conn);
                cmd_getMaxAthleteRank.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                SqlDataAdapter adp_getMaxAthleteRank = new SqlDataAdapter(cmd_getMaxAthleteRank);
                DataTable dt_getMaxAthleteRank = new DataTable();
                adp_getMaxAthleteRank.Fill(dt_getMaxAthleteRank);

                if ((int)dt_getMaxAthleteRank.Rows[0][0] > 1)
                {
                    DataTable dt_getAthleteSessionsPassed;
                    int rankCounter = 1;

                    do
                    {
                        string str_getAthleteSessionsPassed = "SELECT [dbo].[GetAthleteSessionsPassed](@athlete_ID, (SELECT [rankDate] " +
                                                              "                                                      FROM [dbo].[GetAthleteRankIdNameDate](@athlete_ID, @rankNum)), (SELECT [rankDate] " +
                                                              "                                                                                                                      FROM [dbo].[GetAthleteRankIdNameDate](@athlete_ID, @rankNum + 1)))";

                        SqlCommand cmd_getAthleteSessionsPassed = new SqlCommand(str_getAthleteSessionsPassed, conn);
                        cmd_getAthleteSessionsPassed.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;
                        cmd_getAthleteSessionsPassed.Parameters.Add("@rankNum", SqlDbType.Int).Value = (int)dt_getMaxAthleteRank.Rows[0][0] - rankCounter++;

                        SqlDataAdapter adp_getAthleteSessionsPassed = new SqlDataAdapter(cmd_getAthleteSessionsPassed);
                        dt_getAthleteSessionsPassed = new DataTable();
                        adp_getAthleteSessionsPassed.Fill(dt_getAthleteSessionsPassed);

                    } while ((int)dt_getAthleteSessionsPassed.Rows[0][0] == 0);

                    return Convert.ToInt32(dt_getAthleteSessionsPassed.Rows[0][0]);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }



        // -------------------- Events --------------------

        private void dgvMainSideAthletePresence_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            try
            {
                if ((int)dgvMainSideAthletePresence["athleteCredit", e.RowIndex].Value < 0)

                    dgvMainSideAthletePresence.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.RosyBrown;

                if (layoutRegisterRank.Visible)
                    cmbRegisterRankAthlete_SelectedIndexChanged(sender, e);
                else if (layoutShop.Visible)
                    cmbStoreAthletesName_SelectedIndexChanged(sender, e);
                else if (layoutReports.Visible)
                {
                    if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportPresence"])
                    {
                        mtxtReportsPresenceStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                        mtxtReportsPresenceEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                        pcbReportsPresence.Image = Properties.Resources.background_text;

                        FillDgvReportsPresence(dgvReportsPresence, "DESC");
                    }
                    else if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportIncome"])
                    {
                        mtxtReportsIncomeStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                        mtxtReportsIncomeEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                        FillDgvReportsIncome(dgvReportsIncome, "DESC");

                        SetDgvColumnColor(dgvReportsIncome, "اعتبار");

                        lblReportsIncomeCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "شارژ").ToString();
                        lblReportsIncomeTotalExam.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "آزمون").ToString();
                        lblReportsIncomeTotalShop.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "فروشگاه").ToString();
                        lblReportsIncomeTotalCourseCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "ثبت نام").ToString();

                        lblReportsIncomeTotalIncome.Text = (Convert.ToInt32(lblReportsIncomeTotalCourseCharge.Text) + Convert.ToInt32(lblReportsIncomeTotalExam.Text) + Convert.ToInt32(lblReportsIncomeTotalShop.Text) + Convert.ToInt32(lblReportsIncomeCharge.Text)).ToString();
                    }
                    else if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportStorage"])
                    {
                        mtxtReportsStorageStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                        mtxtReportsStorageEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                        FillDgvReportsStorage(dgvReportsStorage, "DESC");
                    }
                }
                else if (layoutPays.Visible)
                    cmbPayAthlete_SelectedIndexChanged(sender, e);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Device

        private void btnConnectDevice_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                if (!timerRestartTimeLong.Enabled && !timerbtnConnectblink.Enabled)
                {
                    if (sdk.GetConnectState())
                    {
                        if (sdk.sta_btnPowerOffDevice() == 1)
                        {
                            btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                            if (btnRegisterSubmit.Text == "بروزرسانی")
                            {
                                pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__17_;
                                gbRegisterFingerprint.Enabled = false;
                            }
                        }
                    }
                    else
                    {
                        if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) == 1)
                        {
                            btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__2_;

                            if (!isAthletesDeactivationDone && dt_getAthletesActivenessState.Rows.Count > 0)
                            {
                                int i = 0;

                                for (i = 0; i < dt_getAthletesActivenessState.Rows.Count; i++)
                                {
                                    if ((int)dt_getAthletesActivenessState.Rows[i]["activeness"] == 0)
                                    {
                                        if (sdk.sta_GetUserInfo((int)dt_getAthletesActivenessState.Rows[i]["athlete_ID"]) == 1 &&
                                            sdk.sta_DeleteEnrollData((int)dt_getAthletesActivenessState.Rows[i]["athlete_ID"], 12) != 1)
                                        {
                                            MessageBox.Show("بروزرسانی وضعیت هنرجویان ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                            break;
                                        }
                                    }
                                }

                                sdk.sta_DisConnect();
                                if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                                {
                                    MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                    btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                                    return;
                                }

                                if (i == dt_getAthletesActivenessState.Rows.Count - 1)
                                {
                                    isAthletesDeactivationDone = true;
                                    dt_getAthletesActivenessState.Dispose();
                                }
                            }

                            sdk.sta_SYNCTime();
                            sdk.sta_DeleteAttLog();

                            if (!gbRegisterFingerprint.Enabled)
                            {
                                int res = sdk.sta_GetUserInfo((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value);

                                if (res == -1)
                                    pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__8_;
                                else if (res == 1)
                                    pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__5_;
                                else
                                    pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;

                                gbRegisterFingerprint.Enabled = true;
                            }
                        }
                        else
                            MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            Cursor = Cursors.Default;
        }

        private void btnRestartDevice_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                if (sdk.GetConnectState())
                {
                    if (sdk.sta_btnRestartDevice() == 1)
                    {
                        btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                        if (btnRegisterSubmit.Text == "بروزرسانی")
                        {
                            pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__17_;
                            gbRegisterFingerprint.Enabled = false;
                        }

                        rotateState = 0;

                        timerbtnRestartDevice.Start();
                        timerRestartTimeLong.Start();
                    }
                }
                else
                    BlinkBtnRestartDevice();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            Cursor = Cursors.Default;
        }



        #endregion

        #region Enrollment - Registration

        #region Enrollment

        // -------------------- Load Register Page --------------------

        private void InitializeRegisterPage(bool resetRegisterPage = true, bool getMaxAthleteID = true)
        {
            try
            {
                GetAthletesName(cmbRegisterName);
                cmbRegisterName.SelectedIndex = -1;

                GetAthletesJob(cmbRegisterJob);
                cmbRegisterJob.SelectedIndex = -1;

                if (resetRegisterPage)
                    ResetRegisterPage();

                if (getMaxAthleteID)
                {
                    maxAthleteID = GetMaxAthleteID();
                    txtRegisterID.Text = maxAthleteID.ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetRegisterPage()
        {
            try
            {
                txtRegisterLastName.ResetText();

                rdbRegisterMale.Checked = true;
                cmbRegisterMarriage.SelectedIndex = 0;
                mtxtRegisterBirthDate.ResetText();
                txtRegisterNCode.ResetText();
                txtRegisterTelephone.ResetText();
                txtRegisterCellphone.ResetText();
                txtRegisterAddress.ResetText();
                cmbRegisterName.ResetText();
                cmbRegisterJob.ResetText();

                pcbRegisterCamera.Image = null;
                pcbRegisterUploadImage.Visible = true;

                tableLayoutPanelRegisterAthleteDetailGraphics.Clear(tableLayoutRegisterAthleteDetail.BackColor);
                toolTipShowMessage.RemoveAll();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- UI Settings --------------------

        private void txtRegisterID_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterLastName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtRegisterBirthDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterNCode_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterJob_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterTelephone_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterCellphone_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterAddress_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterName_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterLastName_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtRegisterBirthDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtRegisterBirthDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtRegisterBirthDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtRegisterBirthDate_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterNCode_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterJob_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterTelephone_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterCellphone_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void layoutRegister_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (formLoaded)
                {
                    if (layoutRegister.Visible)
                        enrollmentDone = sdk.enrollmentDone = false;
                    else
                    {
                        if (gbRegisterFingerprint.Enabled)
                        {
                            if (sdk.timerBlinkFingerprintImage.Enabled)
                                sdk.timerBlinkFingerprintImage.Stop();
                            pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;
                        }
                        else
                        {
                            pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;
                            gbRegisterFingerprint.Enabled = true;
                        }


                        if (!enrollmentDone && sdk.enrollmentDone)
                        {
                            sdk.sta_DisConnect();
                            if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                            {
                                MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                                lblRegister.Text = "تعریف هنرجو";
                                btnRegisterSubmit.Text = "ثبت نهایی";

                                return;
                            }

                            if (btnRegisterSubmit.Text == "ثبت نهایی")
                            {
                                if (sdk.sta_GetUserInfo(maxAthleteID) == 1 &&
                                    sdk.sta_DeleteEnrollData(maxAthleteID, 12) != 1)
                                    MessageBox.Show("بروزرسانی دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                            }
                            else
                            {
                                if (sdk.sta_GetUserInfo((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value) == 1 &&
                                    sdk.sta_DeleteEnrollData((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, 12) != 1)
                                    MessageBox.Show("بروزرسانی دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                            }

                            sdk.sta_DisConnect();
                            if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                            {
                                MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                                lblRegister.Text = "تعریف هنرجو";
                                btnRegisterSubmit.Text = "ثبت نهایی";

                                return;
                            }
                        }

                        if (btnRegisterSubmit.Text == "بروزرسانی")
                        {
                            btnRegisterSubmit.Text = "ثبت نهایی";
                            lblRegister.Text = "تعریف هنرجو";
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------

        public static void SetLanguage(string language)
        {
            try
            {
                if (language.Trim().ToLower() == "fa")
                {
                    CultureInfo fa = new CultureInfo("fa-IR");
                    InputLanguage.CurrentInputLanguage = InputLanguage.FromCulture(fa);
                }
                else
                {
                    CultureInfo en = new CultureInfo("en-us");
                    InputLanguage.CurrentInputLanguage = InputLanguage.FromCulture(en);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private string RemoveExtraWhiteSpaces(string s)
        {
            try
            {
                s = s.Trim();

                if (s.Contains(' '))
                {
                    for (int i = s.IndexOf(' '); i < s.Length; i++)
                    {
                        if (s[i] == ' ')
                        {
                            int hold_i = i, whiteSpacesLength = 0;

                            while (s[++i] == ' ')
                                whiteSpacesLength++;

                            s = s.Remove(hold_i + 1, whiteSpacesLength);

                            i = hold_i;
                        }
                    }
                }

                return s;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        private int GetJobID(string job)
        {
            try
            {
                job = RemoveExtraWhiteSpaces(job);

                string str_getJobID = "SELECT [job_ID] " +
                                      "FROM [tblJob] " +
                                      "WHERE [title] = @title";

                SqlCommand cmd_getJobID = new SqlCommand(str_getJobID, conn);
                cmd_getJobID.Parameters.Add("@title", SqlDbType.NVarChar).Value = job;

                SqlDataAdapter adp_getJobID = new SqlDataAdapter(cmd_getJobID);
                DataTable dt_getJobID = new DataTable();
                adp_getJobID.Fill(dt_getJobID);

                if (dt_getJobID.Rows.Count > 0)
                    return Convert.ToInt32(dt_getJobID.Rows[0][0]);

                string str_newJob = "INSERT INTO [tblJob] " +
                                    "VALUES (@title)";

                SqlCommand cmd_newJob = new SqlCommand(str_newJob, conn);
                cmd_newJob.Parameters.Add("@title", SqlDbType.NVarChar).Value = job;

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                    cmd_newJob.ExecuteNonQuery();
                    conn.Close();
                }
                else
                    cmd_newJob.ExecuteNonQuery();

                string str_getMaxJobID = "SELECT IDENT_CURRENT('tblJob')";

                SqlCommand cmd_getMaxJobID = new SqlCommand(str_getMaxJobID, conn);

                SqlDataAdapter adp_getMaxJobID = new SqlDataAdapter(cmd_getMaxJobID);
                DataTable dt_getMaxJobID = new DataTable();
                adp_getMaxJobID.Fill(dt_getMaxJobID);

                return Convert.ToInt32(dt_getMaxJobID.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            if (conn.State == ConnectionState.Open)
                conn.Close();

            return -1;
        }

        private int GetNameID(string name)
        {
            try
            {
                name = RemoveExtraWhiteSpaces(name);

                string str_getNameID = "SELECT [name_ID] " +
                                       "FROM [tblName] " +
                                       "WHERE [name_title] = @name_title";

                SqlCommand cmd_getNameID = new SqlCommand(str_getNameID, conn);
                cmd_getNameID.Parameters.Add("@name_title", SqlDbType.NVarChar).Value = name;

                SqlDataAdapter adp_getNameID = new SqlDataAdapter(cmd_getNameID);
                DataTable dt_getNameID = new DataTable();
                adp_getNameID.Fill(dt_getNameID);

                if (dt_getNameID.Rows.Count > 0)
                    return Convert.ToInt32(dt_getNameID.Rows[0][0]);

                string str_newName = "INSERT INTO [tblName] " +
                                     "VALUES (@name_title)";

                SqlCommand cmd_newName = new SqlCommand(str_newName, conn);
                cmd_newName.Parameters.Add("@name_title", SqlDbType.NVarChar).Value = name;


                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                    cmd_newName.ExecuteNonQuery();
                    conn.Close();
                }
                else
                    cmd_newName.ExecuteNonQuery();

                string str_getMaxNameID = "SELECT IDENT_CURRENT('tblName')";

                SqlCommand cmd_getMaxNameID = new SqlCommand(str_getMaxNameID, conn);

                SqlDataAdapter adp_getMaxNameID = new SqlDataAdapter(cmd_getMaxNameID);
                DataTable dt_getMaxNameID = new DataTable();
                adp_getMaxNameID.Fill(dt_getMaxNameID);

                return Convert.ToInt32(dt_getMaxNameID.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            if (conn.State == ConnectionState.Open)
                conn.Close();

            return -1;
        }

        private void GetAthletesName(ComboBox cmb)
        {
            try
            {
                string str_getNameTitle = "SELECT [name_title], [name_ID] " +
                                          "FROM [tblName] " +
                                          "ORDER BY [name_title]";

                SqlCommand cmd_getNameTitle = new SqlCommand(str_getNameTitle, conn);

                SqlDataAdapter adp_getNameTitle = new SqlDataAdapter(cmd_getNameTitle);
                DataTable dt_getNameTitle = new DataTable();
                adp_getNameTitle.Fill(dt_getNameTitle);

                if (dt_getNameTitle.Rows.Count > 0)
                {
                    cmb.ValueMember = "name_ID";
                    cmb.DisplayMember = "name_title";
                    cmb.DataSource = dt_getNameTitle;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void GetAthletesJob(ComboBox cmb)
        {
            try
            {
                string str_getJobTitle = "SELECT [title], [job_ID] " +
                                         "FROM [tblJob] " +
                                         "ORDER BY [title]";

                SqlCommand cmd_getJobTitle = new SqlCommand(str_getJobTitle, conn);

                SqlDataAdapter adp_getJobTitle = new SqlDataAdapter(cmd_getJobTitle);
                DataTable dt_getJobTitle = new DataTable();
                adp_getJobTitle.Fill(dt_getJobTitle);

                if (dt_getJobTitle.Rows.Count > 0)
                {
                    cmb.ValueMember = "job_ID";
                    cmb.DisplayMember = "title";
                    cmb.DataSource = dt_getJobTitle;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private int GetMaxAthleteID()
        {
            try
            {
                string str_getMaxAthleteID = "SELECT IDENT_CURRENT('tblAthlete')";

                SqlCommand cmd_getMaxAthleteID = new SqlCommand(str_getMaxAthleteID, conn);

                SqlDataAdapter adp_getMaxAthleteID = new SqlDataAdapter(cmd_getMaxAthleteID);
                DataTable dt_getMaxAthleteID = new DataTable();
                adp_getMaxAthleteID.Fill(dt_getMaxAthleteID);

                int getMaxAthleteID_tmp = Convert.ToInt32(dt_getMaxAthleteID.Rows[0][0]);

                if (getMaxAthleteID_tmp == 1)
                {
                    if (CheckMaxAthleteID())
                        return getMaxAthleteID_tmp + 1;
                    else
                        return 1;
                }
                else
                    return getMaxAthleteID_tmp + 1;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private bool CheckMaxAthleteID()
        {
            try
            {
                string str_checkMaxAthleteID = "SELECT TOP(1) 1 " +
                                               "FROM [tblSellFactor]";

                SqlCommand cmd_checkMaxAthleteID = new SqlCommand(str_checkMaxAthleteID, conn);

                SqlDataAdapter adp_checkMaxAthleteID = new SqlDataAdapter(cmd_checkMaxAthleteID);
                DataTable dt_checkMaxAthleteID = new DataTable();
                adp_checkMaxAthleteID.Fill(dt_checkMaxAthleteID);

                if (dt_checkMaxAthleteID.Rows.Count > 0)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return false;
        }

        public static Image GetImage(object picData)
        {
            try
            {
                Byte[] data = new Byte[0];
                data = (Byte[])picData;
                MemoryStream ms = new MemoryStream(data);

                return Image.FromStream(ms);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        public static byte[] SetImage(PictureBox pic)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters jpegParms = new EncoderParameters(1);
                jpegParms.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                pic.Image.Save(ms, codec, jpegParms);

                return ms.ToArray();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }



        // -------------------- Main Codes --------------------


        // -----> Reset Info

        private void btnRegisterReset_Click(object sender, EventArgs e)
        {
            try
            {
                if (sdk.timerBlinkFingerprintImage.Enabled)
                    sdk.timerBlinkFingerprintImage.Stop();

                ResetRegisterPage();

                if (btnRegisterSubmit.Text == "ثبت نهایی")
                    pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -----> Camera

        private void btnRegisterOpenCamera_Click(object sender, EventArgs e)
        {
            try
            {
                if (FTakePhoto.ConfigCapture() == -1)
                {
                    MessageBox.Show("فایل های مربوط به عکس برداری در مسیر نصب برنامه یافت نشد", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    Application.Exit();
                    return;
                }

                FTakePhoto takePhoto = new FTakePhoto();
                takePhoto.ShowDialog();

                if (!takePhoto.operationCanceled)
                {
                    pcbRegisterCamera.Image = takePhoto.cropedImage;
                    pcbRegisterUploadImage.Visible = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegisterRemoveImage_Click(object sender, EventArgs e)
        {
            try
            {
                pcbRegisterCamera.Image = null;
                pcbRegisterUploadImage.Visible = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void pcbRegisterUploadImage_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog browsePic = new OpenFileDialog();
                browsePic.Filter = "Choose Image(*.jpg;*.png;)|*.jpg;*.png;";

                if (browsePic.ShowDialog() == DialogResult.OK)
                {
                    pcbRegisterUploadImage.Visible = false;
                    pcbRegisterCamera.Image = Image.FromFile(browsePic.FileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -----> Fingerprint Enrollment

        private void btnRegisterFingerprint_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                if (btnRegisterSubmit.Text == "ثبت نهایی")
                {
                    if (sdk.GetConnectState())
                        sdk.sta_OnlineEnroll(maxAthleteID, 6, 1);
                    else
                        BlinkBtnRestartDevice();
                }
                else
                {
                    if (sdk.GetConnectState())
                        sdk.sta_OnlineEnroll((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, 6, 1);
                    else
                        BlinkBtnRestartDevice();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            Cursor = Cursors.Default;
        }



        // -----> Registering & Editing Operations

        private void btnRegisterSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                if (!sdk.timerBlinkFingerprintImage.Enabled)
                {
                    DateTime now = DateTime.Now, birthDate = default(DateTime);
                    int errorsCounter = 0;

                    tableLayoutPanelRegisterAthleteDetailGraphics.Clear(tableLayoutRegisterAthleteDetail.BackColor);
                    toolTipShowMessage.RemoveAll();

                    if (cmbRegisterName.Text.Trim() == "")
                    {
                        tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, cmbRegisterName.Location.X, cmbRegisterName.Location.Y, cmbRegisterName.Width, cmbRegisterName.Height);
                        toolTipShowMessage.SetToolTip(cmbRegisterName, ".نام هنرجو خالی است");

                        errorsCounter++;
                    }

                    if (txtRegisterLastName.Text.Trim() == "")
                    {
                        tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, txtRegisterLastName.Location.X, txtRegisterLastName.Location.Y, txtRegisterLastName.Width, txtRegisterLastName.Height);
                        toolTipShowMessage.SetToolTip(txtRegisterLastName, ".نام خانوادگی هنرجو خالی است");

                        errorsCounter++;
                    }

                    if (mtxtRegisterBirthDate.Text != "   /    / ")
                    {
                        birthDate = ConvertDateTime.sh2m(mtxtRegisterBirthDate.Text);
                        DateTime birthDateLimit = new DateTime(now.Year - 3, now.Month, now.Day, new System.Globalization.GregorianCalendar());

                        if (birthDate == default(DateTime))
                        {
                            tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, mtxtRegisterBirthDate.Location.X, mtxtRegisterBirthDate.Location.Y, mtxtRegisterBirthDate.Width, mtxtRegisterBirthDate.Height);
                            toolTipShowMessage.SetToolTip(mtxtRegisterBirthDate, ".تاریخ تولد نا معتبر است");

                            errorsCounter++;
                        }

                        if (DateTime.Compare(birthDate, now) > 0)
                        {
                            tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, mtxtRegisterBirthDate.Location.X, mtxtRegisterBirthDate.Location.Y, mtxtRegisterBirthDate.Width, mtxtRegisterBirthDate.Height);
                            toolTipShowMessage.SetToolTip(mtxtRegisterBirthDate, ".تاریخ از تاریخ امروز گذشته است");

                            errorsCounter++;
                        }

                        if (DateTime.Compare(birthDate, birthDateLimit) > 0)
                        {
                            tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, mtxtRegisterBirthDate.Location.X, mtxtRegisterBirthDate.Location.Y, mtxtRegisterBirthDate.Width, mtxtRegisterBirthDate.Height);
                            toolTipShowMessage.SetToolTip(mtxtRegisterBirthDate, ".تاریخ تولد در بازه تعیین شده نیست");

                            errorsCounter++;
                        }
                    }

                    if (txtRegisterNCode.Text != "" && txtRegisterNCode.Text.Length < txtRegisterNCode.MaxLength)
                    {
                        tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, txtRegisterNCode.Location.X, txtRegisterNCode.Location.Y, txtRegisterNCode.Width, txtRegisterNCode.Height);
                        toolTipShowMessage.SetToolTip(txtRegisterNCode, ".کد ملی نا معتبر است");

                        errorsCounter++;
                    }

                    if (txtRegisterTelephone.Text != "" && txtRegisterTelephone.Text.Length < txtRegisterTelephone.MaxLength)
                    {
                        tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, txtRegisterTelephone.Location.X, txtRegisterTelephone.Location.Y, txtRegisterTelephone.Width, txtRegisterTelephone.Height);
                        toolTipShowMessage.SetToolTip(txtRegisterTelephone, ".تلفن ثابت نا معتبر است");

                        errorsCounter++;
                    }

                    if (txtRegisterCellphone.Text != "" && txtRegisterCellphone.Text.Length < txtRegisterCellphone.MaxLength)
                    {
                        tableLayoutPanelRegisterAthleteDetailGraphics.DrawRectangle(redPen, txtRegisterCellphone.Location.X, txtRegisterCellphone.Location.Y, txtRegisterCellphone.Width, txtRegisterCellphone.Height);
                        toolTipShowMessage.SetToolTip(txtRegisterCellphone, ".تلفن همراه نا معتبر است");

                        errorsCounter++;
                    }

                    if (errorsCounter == 0)
                    {
                        if (btnRegisterSubmit.Text == "ثبت نهایی")
                        {
                            if (sdk.GetConnectState() && sdk.enrollmentDone)
                            {
                                sdk.sta_DisConnect();
                                if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                                {
                                    MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                    btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;
                                }
                                else
                                {
                                    if (sdk.sta_SetUserInfo(maxAthleteID.ToString(), "User " + maxAthleteID.ToString()) != 1)
                                        MessageBox.Show("ثبت اطلاعات کاربر در دستگاه انجام نشد", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                }
                            }

                            string lastName = RemoveExtraWhiteSpaces(txtRegisterLastName.Text);

                            bool isMale = rdbRegisterMale.Checked ? true : false;

                            object birthDateValue = mtxtRegisterBirthDate.Text == "   /    / " ? DBNull.Value : (object)birthDate;
                            object nCode = txtRegisterNCode.Text == "" ? DBNull.Value : (object)txtRegisterNCode.Text;
                            object jobID = cmbRegisterJob.Text.Trim() == "" ? DBNull.Value : (object)GetJobID(cmbRegisterJob.Text);
                            object telephone = txtRegisterTelephone.Text == "" ? DBNull.Value : (object)txtRegisterTelephone.Text;
                            object cellphone = txtRegisterCellphone.Text == "" ? DBNull.Value : (object)txtRegisterCellphone.Text;
                            object address = txtRegisterAddress.Text.Trim() == "" ? DBNull.Value : (object)txtRegisterAddress.Text.Trim();

                            string str_newAthlete = "INSERT INTO [tblAthlete] " +
                                                    "([name_ID], [l_name], [ismale], [ismarried], [birth_date], [n_code], " +
                                                    "[job_ID], [telephone], [cellphone], [athlete_address], [isactive], [register_date]) " +
                                                    "VALUES (@name_ID, @l_name, @ismale, @ismarried, @birth_date, @n_code, " +
                                                    "@job_ID, @telephone, @cellphone, @athlete_address, @isactive, @register_date)";

                            SqlCommand cmd_newAthlete = new SqlCommand(str_newAthlete, conn);
                            cmd_newAthlete.Parameters.Add("@name_ID", SqlDbType.Int).Value = GetNameID(cmbRegisterName.Text);
                            cmd_newAthlete.Parameters.Add("@l_name", SqlDbType.NVarChar).Value = lastName;
                            cmd_newAthlete.Parameters.Add("@ismale", SqlDbType.Bit).Value = isMale;
                            cmd_newAthlete.Parameters.Add("@ismarried", SqlDbType.Bit).Value = cmbRegisterMarriage.SelectedIndex;
                            cmd_newAthlete.Parameters.Add("@birth_date", SqlDbType.Date).Value = birthDateValue;
                            cmd_newAthlete.Parameters.Add("@n_code", SqlDbType.Char).Value = nCode;
                            cmd_newAthlete.Parameters.Add("@job_ID", SqlDbType.Int).Value = jobID;
                            cmd_newAthlete.Parameters.Add("@telephone", SqlDbType.VarChar).Value = telephone;
                            cmd_newAthlete.Parameters.Add("@cellphone", SqlDbType.VarChar).Value = cellphone;
                            cmd_newAthlete.Parameters.Add("@athlete_address", SqlDbType.NVarChar).Value = address;
                            cmd_newAthlete.Parameters.Add("@isactive", SqlDbType.Bit).Value = 1;
                            cmd_newAthlete.Parameters.Add("@register_date", SqlDbType.DateTime2).Value = now;

                            conn.Open();
                            cmd_newAthlete.ExecuteNonQuery();

                            if (pcbRegisterCamera.Image != null)
                            {
                                string str_newAthletePic = "INSERT INTO [tblAthletePicture] " +
                                                           "VALUES (@athlete_ID, @picture_data)";

                                SqlCommand cmd_newAthletePic = new SqlCommand(str_newAthletePic, conn);
                                cmd_newAthletePic.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = maxAthleteID;
                                cmd_newAthletePic.Parameters.Add("@picture_data", SqlDbType.VarBinary).Value = SetImage(pcbRegisterCamera);

                                cmd_newAthletePic.ExecuteNonQuery();
                            }

                            conn.Close();

                            MessageBox.Show("اطلاعات کاربر جدید با موفقیت ثبت شد\n... انتقال به صفحه ثبت نام", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            enrollmentDone = true;

                            btnRegisterRank_Click(sender, e);
                            cmbRegisterRankAthlete.SelectedValue = maxAthleteID;

                            InitializeRegisterPage();
                        }
                        else
                        {
                            if (sdk.GetConnectState() && sdk.enrollmentDone)
                            {
                                sdk.sta_DisConnect();
                                if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                                {
                                    MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                    btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;
                                }
                                else
                                {
                                    string id = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value.ToString();
                                    if (sdk.sta_SetUserInfo(id, "User " + id) != 1)
                                        MessageBox.Show("ویرایش اطلاعات کاربر در دستگاه انجام نشد", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                }
                            }

                            string lastName = RemoveExtraWhiteSpaces(txtRegisterLastName.Text);

                            object is_male;

                            if (rdbRegisterMale.Checked)
                                is_male = true;
                            else if (rdbRegisterFemale.Checked)
                                is_male = false;
                            else
                                is_male = DBNull.Value;

                            object birthDateValue = mtxtRegisterBirthDate.Text == "   /    / " ? DBNull.Value : (object)birthDate;
                            object nCode = txtRegisterNCode.Text == "" ? DBNull.Value : (object)txtRegisterNCode.Text;
                            object jobID = cmbRegisterJob.Text.Trim() == "" ? DBNull.Value : (object)GetJobID(cmbRegisterJob.Text);
                            object telephone = txtRegisterTelephone.Text == "" ? DBNull.Value : (object)txtRegisterTelephone.Text;
                            object cellphone = txtRegisterCellphone.Text == "" ? DBNull.Value : (object)txtRegisterCellphone.Text;
                            object address = txtRegisterAddress.Text.Trim() == "" ? DBNull.Value : (object)txtRegisterAddress.Text.Trim();

                            string str_updateAthlete = "UPDATE [tblAthlete] " +
                                                       "SET [name_ID] = @name_ID, [l_name] = @l_name, [ismale] = @ismale, " +
                                                       "    [ismarried] = @ismarried, [birth_date] = @birth_date, [n_code] = @n_code, [job_ID] = @job_ID, " +
                                                       "    [telephone] = @telephone, [cellphone] = @cellphone, [athlete_address] = @athlete_address " +
                                                       "WHERE athlete_ID = @athlete_ID";

                            SqlCommand cmd_updateAthlete = new SqlCommand(str_updateAthlete, conn);
                            cmd_updateAthlete.Parameters.Add("@name_ID", SqlDbType.Int).Value = GetNameID(cmbRegisterName.Text);
                            cmd_updateAthlete.Parameters.Add("@l_name", SqlDbType.NVarChar).Value = lastName;
                            cmd_updateAthlete.Parameters.Add("@ismale", SqlDbType.Bit).Value = is_male;
                            cmd_updateAthlete.Parameters.Add("@ismarried", SqlDbType.Bit).Value = cmbRegisterMarriage.SelectedIndex;
                            cmd_updateAthlete.Parameters.Add("@birth_date", SqlDbType.Date).Value = birthDateValue;
                            cmd_updateAthlete.Parameters.Add("@n_code", SqlDbType.Char).Value = nCode;
                            cmd_updateAthlete.Parameters.Add("@job_ID", SqlDbType.Int).Value = jobID;
                            cmd_updateAthlete.Parameters.Add("@telephone", SqlDbType.VarChar).Value = telephone;
                            cmd_updateAthlete.Parameters.Add("@cellphone", SqlDbType.VarChar).Value = cellphone;
                            cmd_updateAthlete.Parameters.Add("@athlete_address", SqlDbType.NVarChar).Value = address;
                            cmd_updateAthlete.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                            conn.Open();
                            cmd_updateAthlete.ExecuteNonQuery();

                            if (pcbRegisterCamera.Image != null)
                            {
                                string str_getAthletePicExistence = "SELECT [athlete_ID] " +
                                                                    "FROM [tblAthletePicture] " +
                                                                    "WHERE [athlete_ID] = @athlete_ID";

                                SqlCommand cmd_getAthletePicExistence = new SqlCommand(str_getAthletePicExistence, conn);
                                cmd_getAthletePicExistence.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                                SqlDataAdapter adp_getAthletePicExistence = new SqlDataAdapter(cmd_getAthletePicExistence);
                                DataTable dt_getAthletePicExistence = new DataTable();
                                adp_getAthletePicExistence.Fill(dt_getAthletePicExistence);

                                if (dt_getAthletePicExistence.Rows.Count > 0)
                                {
                                    string str_updateAthletePic = "UPDATE [tblAthletePicture] " +
                                                                  "SET [picture_data] = @picture_data " +
                                                                  "WHERE athlete_ID = @athlete_ID";

                                    SqlCommand cmd_updateAthletePic = new SqlCommand(str_updateAthletePic, conn);
                                    cmd_updateAthletePic.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;
                                    cmd_updateAthletePic.Parameters.Add("@picture_data", SqlDbType.VarBinary).Value = SetImage(pcbRegisterCamera);

                                    cmd_updateAthletePic.ExecuteNonQuery();
                                }
                                else
                                {
                                    string str_newAthletePic = "INSERT INTO [tblAthletePicture] " +
                                                               "VALUES (@athlete_ID, @picture_data)";

                                    SqlCommand cmd_newAthletePic = new SqlCommand(str_newAthletePic, conn);
                                    cmd_newAthletePic.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;
                                    cmd_newAthletePic.Parameters.Add("@picture_data", SqlDbType.VarBinary).Value = SetImage(pcbRegisterCamera);

                                    cmd_newAthletePic.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string str_deleteAthletePic = "DELETE FROM [tblAthletePicture] " +
                                                              "WHERE [athlete_ID] = @athlete_ID";

                                SqlCommand cmd_deleteAthletePic = new SqlCommand(str_deleteAthletePic, conn);
                                cmd_deleteAthletePic.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                                cmd_deleteAthletePic.ExecuteNonQuery();
                            }

                            conn.Close();

                            MessageBox.Show("ویرایش اطلاعات کاربر با موفقیت انجام شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            enrollmentDone = true;

                            object athlete = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;
                            btnReports_Click(sender, e);

                            SearchDgvSelect(athlete, dgvReportsAthletes);

                            InitializeRegisterPage(true, false);
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -----> Loading Athlete Info To Edit

        private void btnReportsAthletesEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReportsAthletes.Rows.Count > 0)
                {
                    string str_getAthleteInfo = "SELECT [tblName].[name_title], [tblAthlete].[l_name], " +
                                                "       [tblAthlete].[ismale], [tblAthlete].[ismarried], [tblAthlete].[birth_date], " +
                                                "       [tblAthlete].[n_code], [tblJob].[title], [tblAthlete].[telephone], [tblAthlete].[cellphone], " +
                                                "       [tblAthlete].[athlete_address], [tblAthletePicture].[picture_data] " +
                                                "FROM [tblAthlete] " +
                                                "INNER JOIN [tblName] ON [tblAthlete].[name_ID] = [tblName].[name_ID] " +
                                                "LEFT JOIN [tblJob] ON [tblAthlete].[job_ID] = [tblJob].[job_ID] " +
                                                "LEFT JOIN [tblAthletePicture] ON [tblAthlete].[athlete_ID] = [tblAthletePicture].[athlete_ID] " +
                                                "WHERE [tblAthlete].[athlete_ID] = @athlete_ID";

                    SqlCommand cmd_getAthleteInfo = new SqlCommand(str_getAthleteInfo, conn);
                    cmd_getAthleteInfo.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                    SqlDataAdapter adp_getAthleteInfo = new SqlDataAdapter(cmd_getAthleteInfo);
                    DataTable dt_getAthleteInfo = new DataTable();
                    adp_getAthleteInfo.Fill(dt_getAthleteInfo);

                    if (dt_getAthleteInfo.Rows.Count > 0)
                    {
                        if (sdk.timerBlinkFingerprintImage.Enabled)
                            sdk.timerBlinkFingerprintImage.Stop();

                        InitializeRegisterPage(false, false);

                        txtRegisterID.Text = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value.ToString();
                        cmbRegisterName.Text = dt_getAthleteInfo.Rows[0]["name_title"].ToString();
                        txtRegisterLastName.Text = dt_getAthleteInfo.Rows[0]["l_name"].ToString();

                        object athleteIsMale = dt_getAthleteInfo.Rows[0]["ismale"];

                        if (Convert.IsDBNull(athleteIsMale))
                            rdbRegisterMale.Checked = rdbRegisterFemale.Checked = false;
                        else if (Convert.ToBoolean(athleteIsMale))
                            rdbRegisterMale.Checked = true;
                        else
                            rdbRegisterFemale.Checked = true;

                        object athleteIsMarried = dt_getAthleteInfo.Rows[0]["ismarried"];

                        if (Convert.IsDBNull(athleteIsMarried))
                            cmbRegisterMarriage.SelectedIndex = 0;
                        else if (Convert.ToBoolean(athleteIsMarried))
                            cmbRegisterMarriage.SelectedIndex = 2;
                        else
                            cmbRegisterMarriage.SelectedIndex = 1;

                        string athleteBirthDate = dt_getAthleteInfo.Rows[0]["birth_date"].ToString();

                        if (athleteBirthDate != "")
                        {
                            DateTime dt_birthDate = Convert.ToDateTime(athleteBirthDate);
                            mtxtRegisterBirthDate.Text = ConvertDateTime.m2sh(dt_birthDate).Substring(0, 10);
                        }
                        else
                            mtxtRegisterBirthDate.ResetText();

                        txtRegisterNCode.Text = dt_getAthleteInfo.Rows[0]["n_code"].ToString();
                        cmbRegisterJob.Text = dt_getAthleteInfo.Rows[0]["title"].ToString();
                        txtRegisterTelephone.Text = dt_getAthleteInfo.Rows[0]["telephone"].ToString();
                        txtRegisterCellphone.Text = dt_getAthleteInfo.Rows[0]["cellphone"].ToString();
                        txtRegisterAddress.Text = dt_getAthleteInfo.Rows[0]["athlete_address"].ToString();

                        if (!Convert.IsDBNull(dt_getAthleteInfo.Rows[0]["picture_data"]))
                        {
                            pcbRegisterCamera.Image = GetImage(dt_getAthleteInfo.Rows[0]["picture_data"]);
                            pcbRegisterUploadImage.Visible = false;
                        }
                        else
                        {
                            pcbRegisterCamera.Image = null;
                            pcbRegisterUploadImage.Visible = true;
                        }

                        if (sdk.GetConnectState())
                        {
                            int res = sdk.sta_GetUserInfo((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value);

                            if (res == -1)
                                pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__8_;
                            else if (res == 1)
                                pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__5_;
                            else
                                pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;
                        }
                        else
                        {
                            pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__17_;
                            gbRegisterFingerprint.Enabled = false;
                        }

                        layoutPays.Visible = layoutRegisterRank.Visible = layoutReports.Visible = layoutSettings.Visible = layoutExam.Visible = layoutShop.Visible = false;

                        btnRegister.Image = Properties.Resources.add_user_green;
                        lblRegister.ForeColor = Color.Green;

                        selectedTab = lblRegister.Text = btnRegisterSubmit.Text = "بروزرسانی";
                        topButtons_MouseLeave(sender, e);

                        layoutRegister.Visible = true;
                    }
                    else
                    {
                        if (sdk.timerBlinkFingerprintImage.Enabled)
                            sdk.timerBlinkFingerprintImage.Stop();
                        pcbRegisterFingerImage.Image = Properties.Resources.fingerprint__15_;

                        InitializeRegisterPage(true, false);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -----> Athlete Deletion

        private void btnReportsAthleteDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReportsAthletes.Rows.Count > 0)
                {
                    DialogResult result = MessageBox.Show("آیا از حذف هنرجو اطمینان دارید ؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    if (result == DialogResult.Yes)
                    {
                        if (sdk.GetConnectState())
                        {
                            if (sdk.sta_GetUserInfo((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value) == 1 &&
                                sdk.sta_DeleteEnrollData((int)dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, 12) != 1)
                            {
                                sdk.sta_DisConnect();
                                if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                                {
                                    MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                    btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                                    return;
                                }

                                MessageBox.Show("حذف اثر انگشت مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                return;
                            }

                            sdk.sta_DisConnect();
                            if (sdk.sta_ConnectTCP(IP, PORT, COMMKEY) != 1)
                            {
                                MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                btnConnectDevice.BackgroundImage = Properties.Resources.wifi_signal_waves__1_;

                                return;
                            }

                            string str_getNameID = "SELECT [name_ID] " +
                                                   "FROM [tblAthlete] " +
                                                   "WHERE [athlete_ID] = @athlete_ID";

                            SqlCommand cmd_getNameID = new SqlCommand(str_getNameID, conn);
                            cmd_getNameID.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                            SqlDataAdapter adp_getNameID = new SqlDataAdapter(cmd_getNameID);
                            DataTable dt_getNameID = new DataTable();
                            adp_getNameID.Fill(dt_getNameID);

                            conn.Open();

                            if (dt_getNameID.Rows.Count > 0)
                            {
                                string str_getNameIDNum = "SELECT COUNT([name_ID]) " +
                                                          "FROM [tblAthlete] " +
                                                          "WHERE [name_ID] = @name_ID";

                                SqlCommand cmd_getNameIDNum = new SqlCommand(str_getNameIDNum, conn);
                                cmd_getNameIDNum.Parameters.Add("@name_ID", SqlDbType.Int).Value = dt_getNameID.Rows[0][0];

                                SqlDataAdapter adp_getNameIDNum = new SqlDataAdapter(cmd_getNameIDNum);
                                DataTable dt_getNameIDNum = new DataTable();
                                adp_getNameIDNum.Fill(dt_getNameIDNum);

                                if ((int)dt_getNameIDNum.Rows[0][0] == 1)
                                {
                                    string str_deleteName = "DELETE FROM [tblName] " +
                                                            "WHERE [name_ID] = @name_ID";

                                    SqlCommand cmd_deleteName = new SqlCommand(str_deleteName, conn);
                                    cmd_deleteName.Parameters.Add("@name_ID", SqlDbType.Int).Value = dt_getNameID.Rows[0][0];

                                    cmd_deleteName.ExecuteNonQuery();
                                }
                            }

                            string str_deleteAthlete = "DELETE FROM [tblAthlete] " +
                                                       "WHERE [athlete_ID] = @athlete_ID";

                            SqlCommand cmd_deleteAthlete = new SqlCommand(str_deleteAthlete, conn);
                            cmd_deleteAthlete.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;

                            cmd_deleteAthlete.ExecuteNonQuery();
                            conn.Close();

                            MessageBox.Show("اطلاعات کاربر مورد نظر با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            dgvReportsAthletes.Rows.RemoveAt(dgvReportsAthletes.CurrentCell.RowIndex);

                            lblReportsAthletesTotalPersons.Text = dgvReportsAthletes.Rows.Count.ToString();
                        }
                        else
                            BlinkBtnRestartDevice();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Registration

        private void cmbRegisterRankAthlete_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterSans_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterRankRank_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtRegisterRankDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterRankCourseSections_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtRegisterRankDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtRegisterRankDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtRegisterRankDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetRegisterRankPage()
        {
            try
            {
                cmbRegisterRankAthlete.SelectedValue = -1;
                cmbRegisterRankAthlete.Text = "انتخاب هنرجو";

                cmbRegisterRankRank.SelectedValue = 0;
                cmbRegisterRankRank.Text = "انتخاب سطح";

                cmbRegisterSans.SelectedValue = -1;
                cmbRegisterSans.Text = "هنرجو انتخاب شود";

                lblRegisterRankRemainSections.Text = "-";
                lblRegisterRankSideAthleteID.Text = "کد";
                lblRegisterRankSideCellphone.Text = "شماره موبایل";
                lblRegisterRankSideCredit.Text = "اعتبار";
                lblRegisterRankSideFullName.Text = "نام و نام خانوادگی";
                lblRegisterRankSideRank.Text = "سطح";
                lblRegisterRankSections.Text = "-";

                btnExamPrintLastRank.Text = "آخرین آزمون";
                btnExamLastExamReturn.Visible = false;

                cmbExamRank.Enabled = true;
                mtxtExamDate.ReadOnly = false;
                rdbAccepted.Enabled = true;
                rdbFailed.Enabled = true;
                txtExamEnglishName.ReadOnly = false;
                txtExamPrice.ReadOnly = false;
                cmbRegisterSans.Enabled = false;
                cmbRegisterRankRank.Enabled = false;


                pcbRegisterRankSideAthleteImage.Image = Properties.Resources.background_text;

                lblRegisterRankSideRank.BackColor = Color.LightGray;
                lblRegisterRankSideRank.ForeColor = SystemColors.ControlText;
                lblRegisterRankSideCredit.ForeColor = SystemColors.ControlText;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion



        #endregion

        #region Store

        // -------------------- Load Store Page --------------------

        private void InitializeStorePage()
        {
            try
            {
                lblStoreSellFactorID.Text = GetMaxSellFactorID().ToString();

                GetAthletesFullName(cmbStoreSellAthletesName);
                cmbStoreSellAthletesName.SelectedValue = -1;
                cmbStoreSellAthletesName.Text = "انتخاب هنرجو";

                limitRedoing = true;

                ResetStorePage();

                GetItemsName(cmbStoreSellItemsName);
                cmbStoreSellItemsName.SelectedIndex = -1;
                cmbStoreSellItemsName.Text = "انتخاب کالا";

                limitRedoing = false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void InitializeStoreManagementPage()
        {
            try
            {
                limitRedoing = true;

                dgvStoreManageItemRegister.Rows.Clear();
                ResetItemDetails(cmbStoreManageItemSizes, cmbStoreManageItemColors, cmbStoreManageItemIsMaleValues);

                GetItemsName(cmbStoreManageItemsNames);
                cmbStoreManageItemsNames.SelectedIndex = -1;

                limitRedoing = false;

                dgvItemRegisterErrorRows = new List<int>();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void InitializeStoreManagementManageItem()
        {
            try
            {
                limitRedoing = true;

                ResetItemDetails(cmbStoreManageItemSizes, cmbStoreManageItemColors, cmbStoreManageItemIsMaleValues);

                GetItemsName(cmbStoreManageItemsNames);
                cmbStoreManageItemsNames.SelectedIndex = -1;

                limitRedoing = false;

                dgvItemRegisterErrorRows = new List<int>();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void InitializeDgvStoreShoppingBag()
        {
            try
            {
                dgvStoreSellShoppingBag.Columns.Add("itemSpecificationID", "کد");
                dgvStoreSellShoppingBag.Columns.Add("itemName", "نام کالا");
                dgvStoreSellShoppingBag.Columns.Add("itemSize", "سایز");
                dgvStoreSellShoppingBag.Columns.Add("itemColor", "رنگ");
                dgvStoreSellShoppingBag.Columns.Add("itemIsMaleValue", "جنسیت");
                dgvStoreSellShoppingBag.Columns.Add("itemNum", "تعداد");
                dgvStoreSellShoppingBag.Columns.Add("itemPrice", "قیمت");

                DataGridViewLinkColumn deleteLink = new DataGridViewLinkColumn
                {
                    Name = "rowDeleteLink",
                    HeaderText = "",
                    Text = "حذف",
                    TrackVisitedState = false,
                    UseColumnTextForLinkValue = true
                };
                dgvStoreSellShoppingBag.Columns.Add(deleteLink);

                dgvStoreSellShoppingBag.Columns["itemSpecificationID"].Visible = false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void InitializeDgvStoreManageItemRegister()
        {
            try
            {
                dgvStoreManageItemRegister.Columns.Add("itemName", "نام کالا");
                dgvStoreManageItemRegister.Columns.Add("itemSize", "سایز");
                dgvStoreManageItemRegister.Columns.Add("itemColor", "رنگ");

                DataGridViewComboBoxColumn cmbItemIsMaleValue = new DataGridViewComboBoxColumn
                {
                    Name = "itemIsMaleValue",
                    HeaderText = "جنسیت",
                    FlatStyle = FlatStyle.Flat,
                };
                cmbItemIsMaleValue.Items.AddRange(new string[] { "", "آقایان", "بانوان" });
                dgvStoreManageItemRegister.Columns.Add(cmbItemIsMaleValue);

                dgvStoreManageItemRegister.Columns.Add("itemNumber", "تعداد");
                dgvStoreManageItemRegister.Columns.Add("itemPrice", "قیمت");

                DataGridViewLinkColumn deleteLink = new DataGridViewLinkColumn
                {
                    Name = "rowDeleteLink",
                    HeaderText = "",
                    Text = "حذف",
                    TrackVisitedState = false,
                    UseColumnTextForLinkValue = true
                };
                dgvStoreManageItemRegister.Columns.Add(deleteLink);

                ((DataGridViewTextBoxColumn)dgvStoreManageItemRegister.Columns["itemName"]).MaxInputLength = 50;
                ((DataGridViewTextBoxColumn)dgvStoreManageItemRegister.Columns["itemSize"]).MaxInputLength = 20;
                ((DataGridViewTextBoxColumn)dgvStoreManageItemRegister.Columns["itemColor"]).MaxInputLength = 20;
                ((DataGridViewTextBoxColumn)dgvStoreManageItemRegister.Columns["itemNumber"]).MaxInputLength = 4;
                ((DataGridViewTextBoxColumn)dgvStoreManageItemRegister.Columns["itemPrice"]).MaxInputLength = 9;

                foreach (DataGridViewColumn column in dgvStoreManageItemRegister.Columns)
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetStorePage()
        {
            try
            {
                pcbStoreSellAthletePic.Image = Properties.Resources.background_text;
                lblStoreSellAthleteID.Text = "کد";
                lblStoreSellAthleteName.Text = "نام و نام خانوادگی";
                lblStoreSellAthleteCellphone.Text = "شماره موبایل";
                lblStoreSellAthleteRank.Text = "سطح";
                lblStoreSellAthleteCredit.Text = "اعتبار";

                lblStoreSellAthleteRank.BackColor = Color.LightGray;
                lblStoreSellAthleteRank.ForeColor = SystemColors.ControlText;
                lblStoreSellAthleteCredit.ForeColor = SystemColors.ControlText;

                ResetItemDetails(cmbStoreSellItemSizes, cmbStoreSellItemColors, cmbStoreSellItemIsMaleValues);

                nudStoreSellItemNumber.Value = 0;
                lblStoreSellItemRemaining.Text = "-";
                lblStoreSellItemPrice.Text = "-";
                lblStoreSellItemsTotalPrice.Text = "-";

                dgvStoreSellShoppingBag.Rows.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetItemDetails(ComboBox cmbItemSizes, ComboBox cmbItemColors, ComboBox cmbItemIsMaleValues)
        {
            try
            {
                cmbItemSizes.DataSource = cmbItemColors.DataSource = cmbItemIsMaleValues.DataSource = null;
                cmbItemSizes.ValueMember = cmbItemColors.ValueMember = cmbItemIsMaleValues.ValueMember = "";
                cmbItemSizes.DisplayMember = cmbItemColors.DisplayMember = cmbItemIsMaleValues.DisplayMember = "";
                cmbItemSizes.Enabled = cmbItemColors.Enabled = cmbItemIsMaleValues.Enabled = false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- UI Settings --------------------

        private void cmbStoreSellAthletesName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreSellItemsName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreSellItemSizes_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreSellItemColors_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreSellItemIsMaleValues_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void nudStoreSellItemNumber_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreManageItemRegister_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtStoreManageItemsNames_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtStoreManageItemSizes_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtStoreManageItemColors_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemDefIsMaleValues_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtStoreManageItemPrice_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemsNames_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemSizes_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemColors_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemIsMaleValues_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreShoppingBag_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvStoreSellShoppingBag.RowCount > 0 &&
                dgvStoreSellShoppingBag.CurrentCell.OwningColumn.Name.Equals("rowDeleteLink"))
                {
                    dgvStoreSellShoppingBag.Rows.RemoveAt(e.RowIndex);

                    if (dgvStoreSellShoppingBag.RowCount > 0)
                        dgvStoreSellShoppingBag.CurrentCell = dgvStoreSellShoppingBag["itemName", 0];
                    else
                        dgvStoreSellShoppingBag.CurrentCell = null;

                    lblStoreSellItemsTotalPrice.Text = GetStoreSellTotalPrice().ToString();
                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreManageItemRegister_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvStoreManageItemRegister.RowCount > 1 &&
                dgvStoreManageItemRegister.CurrentCell.OwningColumn.Name.Equals("rowDeleteLink"))
                    dgvStoreManageItemRegister.Rows.RemoveAt(e.RowIndex);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreManageItemRegister_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvStoreManageItemRegister["itemName", e.RowIndex].Value != null && dgvStoreManageItemRegister.Focused)
                {
                    string itemName = dgvStoreManageItemRegister["itemName", e.RowIndex].Value.ToString();
                    itemName = RemoveExtraWhiteSpaces(itemName);

                    int itemID = GetItemID(itemName);

                    if (itemID != -1)
                    {
                        string itemSize = "", itemColor = "", itemIsMaleValue = "";

                        if (dgvStoreManageItemRegister["itemSize", e.RowIndex].Value != null)
                        {
                            itemSize = dgvStoreManageItemRegister["itemSize", e.RowIndex].Value.ToString();
                            itemSize = RemoveExtraWhiteSpaces(itemSize.ToString());
                        }

                        if (dgvStoreManageItemRegister["itemColor", e.RowIndex].Value != null)
                        {
                            itemColor = dgvStoreManageItemRegister["itemColor", e.RowIndex].Value.ToString();
                            itemColor = RemoveExtraWhiteSpaces(itemColor);
                        }

                        if (dgvStoreManageItemRegister["itemIsMaleValue", e.RowIndex].Value != null)
                            itemIsMaleValue = dgvStoreManageItemRegister["itemIsMaleValue", e.RowIndex].Value.ToString();

                        itemSpecificationID = GetItemSpecificationID(itemID, itemSize, itemColor, itemIsMaleValue);

                        if (dgvStoreManageItemRegister["itemPrice", e.RowIndex].ReadOnly && itemSpecificationID == -1)
                        {
                            dgvStoreManageItemRegister["itemPrice", e.RowIndex].Value = null;
                            dgvStoreManageItemRegister["itemPrice", e.RowIndex].ReadOnly = false;

                        }
                        else if (itemSpecificationID != -1)
                        {
                            dgvStoreManageItemRegister["itemPrice", e.RowIndex].Value = GetItemPrice(itemSpecificationID);
                            dgvStoreManageItemRegister["itemPrice", e.RowIndex].ReadOnly = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtStoreManageItemPrice_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreManageItemRegister_numericColumns_KeyPress(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvStoreManageItemRegister_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                if (e.Control is TextBox tb)
                {
                    string colName = dgvStoreManageItemRegister.CurrentCell.OwningColumn.Name;

                    if (!(colName.Equals("itemNumber") || colName.Equals("itemPrice")) && eventSubscribed)
                    {
                        tb.KeyDown -= new KeyEventHandler(dgvStoreManageItemRegister_numericColumns_KeyPress);
                        eventSubscribed = false;
                    }

                    if (colName.Equals("itemName"))
                    {
                        tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;

                        string str_getItems = "SELECT [item_name] " +
                                              "FROM [tblItem] " +
                                              "ORDER BY [item_name]";

                        tb.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getItems, conn));

                        tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    }
                    else if (colName.Equals("itemSize"))
                    {
                        tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;

                        string str_getItemsSizes = "SELECT DISTINCT[size] " +
                                                   "FROM [tblItemSpecification] " +
                                                   "WHERE [size] IS NOT NULL " +
                                                   "ORDER BY [size]";

                        tb.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getItemsSizes, conn));

                        tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    }
                    else if (colName.Equals("itemColor"))
                    {
                        tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;

                        string str_getitemsColors = "SELECT [color_name] " +
                                                    "FROM [tblColor] " +
                                                    "ORDER BY [color_name]";

                        tb.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getitemsColors, conn));

                        tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    }
                    else if ((colName.Equals("itemNumber") || colName.Equals("itemPrice")) && !eventSubscribed)
                    {
                        tb.KeyDown += new KeyEventHandler(dgvStoreManageItemRegister_numericColumns_KeyPress);
                        eventSubscribed = true;
                    }
                    else
                    {
                        tb.AutoCompleteMode = AutoCompleteMode.None;
                        tb.AutoCompleteCustomSource = null;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------

        private int GetMaxSellFactorID()
        {
            try
            {
                string str_getMaxSellFactorID = "SELECT IDENT_CURRENT('tblSellFactor')";

                SqlCommand cmd_getMaxSellFactorID = new SqlCommand(str_getMaxSellFactorID, conn);

                SqlDataAdapter adp_getMaxSellFactorID = new SqlDataAdapter(cmd_getMaxSellFactorID);
                DataTable dt_getMaxSellFactorID = new DataTable();
                adp_getMaxSellFactorID.Fill(dt_getMaxSellFactorID);

                int getMaxSellFactorID_tmp = Convert.ToInt32(dt_getMaxSellFactorID.Rows[0][0]);

                if (getMaxSellFactorID_tmp == 1)
                {
                    if (CheckMaxSellFactorID())
                        return getMaxSellFactorID_tmp + 1;
                    else
                        return 1;
                }
                else
                    return getMaxSellFactorID_tmp + 1;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private int GetMaxItemID()
        {
            try
            {
                string str_getMaxItemID = "SELECT IDENT_CURRENT('tblItem')";

                SqlCommand cmd_getMaxItemID = new SqlCommand(str_getMaxItemID, conn);

                SqlDataAdapter adp_getMaxItemID = new SqlDataAdapter(cmd_getMaxItemID);
                DataTable dt_getMaxItemID = new DataTable();
                adp_getMaxItemID.Fill(dt_getMaxItemID);

                int getMaxItemID_tmp = Convert.ToInt32(dt_getMaxItemID.Rows[0][0]);

                if (getMaxItemID_tmp == 1)
                {
                    if (CheckMaxItemID())
                        return getMaxItemID_tmp + 1;
                    else
                        return 1;
                }
                else
                    return getMaxItemID_tmp + 1;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private bool CheckMaxSellFactorID()
        {
            try
            {
                string str_checkMaxSellFactorID = "SELECT TOP(1) 1 " +
                                                  "FROM [tblSellFactor]";

                SqlCommand cmd_checkMaxSellFactorID = new SqlCommand(str_checkMaxSellFactorID, conn);

                SqlDataAdapter adp_checkMaxSellFactorID = new SqlDataAdapter(cmd_checkMaxSellFactorID);
                DataTable dt_checkMaxSellFactorID = new DataTable();
                adp_checkMaxSellFactorID.Fill(dt_checkMaxSellFactorID);

                if (dt_checkMaxSellFactorID.Rows.Count > 0)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return false;
        }

        private bool CheckMaxItemID()
        {
            try
            {
                string str_checkMaxItemID = "SELECT TOP(1) 1 " +
                                            "FROM [tblItem]";

                SqlCommand cmd_checkMaxItemID = new SqlCommand(str_checkMaxItemID, conn);

                SqlDataAdapter adp_checkMaxItemID = new SqlDataAdapter(cmd_checkMaxItemID);
                DataTable dt_checkMaxItemID = new DataTable();
                adp_checkMaxItemID.Fill(dt_checkMaxItemID);

                if (dt_checkMaxItemID.Rows.Count > 0)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return false;
        }

        private void GetAthletesFullName(ComboBox cmb)
        {
            try
            {
                string str_getAthletesFullName = "SELECT [tblAthlete].[athlete_ID], [tblName].[name_title] + ' ' + [tblAthlete].[l_name] + " +
                                                 "       ' - ' + CONVERT(VARCHAR, [tblAthlete].[athlete_ID]) AS [athleteName] " +
                                                 "FROM [tblAthlete] " +
                                                 "INNER JOIN [tblName] ON [tblAthlete].[name_ID] = [tblName].[name_ID] " +
                                                 "ORDER BY [tblName].[name_title], [tblAthlete].[l_name]";

                SqlCommand cmd_getAthletesFullName = new SqlCommand(str_getAthletesFullName, conn);

                SqlDataAdapter adp_getAthletesFullName = new SqlDataAdapter(cmd_getAthletesFullName);
                DataTable dt_getAthletesFullName = new DataTable();
                adp_getAthletesFullName.Fill(dt_getAthletesFullName);

                if (dt_getAthletesFullName.Rows.Count > 0)
                {
                    cmb.ValueMember = "athlete_ID";
                    cmb.DisplayMember = "athleteName";
                    cmb.DataSource = dt_getAthletesFullName;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
                cmb.SelectedValue = -1;
                cmb.ResetText();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void GetItemsName(ComboBox cmb)
        {
            try
            {
                string str_getItemsNames = "SELECT [item_name], [item_ID] " +
                                           "FROM [tblItem] " +
                                           "ORDER BY [item_name]";

                SqlCommand cmd_getItemsNames = new SqlCommand(str_getItemsNames, conn);

                SqlDataAdapter adp_getItemsNames = new SqlDataAdapter(cmd_getItemsNames);
                DataTable dt_getItemsNames = new DataTable();
                adp_getItemsNames.Fill(dt_getItemsNames);

                if (dt_getItemsNames.Rows.Count > 0)
                {
                    cmb.ValueMember = "item_ID";
                    cmb.DisplayMember = "item_name";
                    cmb.DataSource = dt_getItemsNames;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private int GetColorID(string color)
        {
            try
            {
                string str_getColorID = "SELECT [color_ID] " +
                                        "FROM [tblColor] " +
                                        "WHERE [color_name] = @color_name";

                SqlCommand cmd_getColorID = new SqlCommand(str_getColorID, conn);
                cmd_getColorID.Parameters.Add("@color_name", SqlDbType.NVarChar).Value = color;

                SqlDataAdapter adp_getColorID = new SqlDataAdapter(cmd_getColorID);
                DataTable dt_getColorID = new DataTable();
                adp_getColorID.Fill(dt_getColorID);

                if (dt_getColorID.Rows.Count > 0)
                    return Convert.ToInt32(dt_getColorID.Rows[0][0]);

                string str_newColor = "INSERT INTO [tblColor] " +
                                      "VALUES (@color_name)";

                SqlCommand cmd_newColor = new SqlCommand(str_newColor, conn);
                cmd_newColor.Parameters.Add("@color_name", SqlDbType.NVarChar).Value = color;

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                    cmd_newColor.ExecuteNonQuery();
                    conn.Close();
                }
                else
                    cmd_newColor.ExecuteNonQuery();

                string str_getMaxColorID = "SELECT IDENT_CURRENT('tblColor')";

                SqlDataAdapter adp_getMaxColorID = new SqlDataAdapter(str_getMaxColorID, conn);
                DataTable dt_getMaxColorID = new DataTable();
                adp_getMaxColorID.Fill(dt_getMaxColorID);

                return Convert.ToInt32(dt_getMaxColorID.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            if (conn.State == ConnectionState.Open)
                conn.Close();

            return -1;
        }

        private int GetItemID(string itemName)
        {
            try
            {
                string str_getItemID = "SELECT [item_ID] " +
                                       "FROM [tblItem] " +
                                       "WHERE [item_name] = @itemName";

                SqlCommand cmd_getItemID = new SqlCommand(str_getItemID, conn);
                cmd_getItemID.Parameters.Add("itemName", SqlDbType.NVarChar).Value = itemName;

                SqlDataAdapter adp_getItemID = new SqlDataAdapter(cmd_getItemID);
                DataTable dt_getItemID = new DataTable();
                adp_getItemID.Fill(dt_getItemID);

                if (dt_getItemID.Rows.Count > 0)
                    return Convert.ToInt32(dt_getItemID.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }


        private int GetItemSpecificationID(int itemID, string itemSize, string itemColor, string itemIsMaleValue)
        {
            try
            {
                itemSize = itemSize == "" ? itemSize = "IS NULL" : "= N'" + itemSize + "'";
                itemColor = itemColor == "" ? itemColor = "IS NULL" : "= N'" + itemColor + "'";
                itemIsMaleValue = itemIsMaleValue == "" || itemIsMaleValue == "هر دو" ? itemIsMaleValue = "IS NULL" : itemIsMaleValue == "آقایان" ? "= 1" : "= 0";

                string str_getItemSpecificationID = "SELECT [tblItemSpecification].[item_specification_ID] " +
                                                    "FROM [tblColor] " +
                                                    "RIGHT JOIN [tblItemSpecification] " +
                                                    "ON [tblColor].[color_ID] = [tblItemSpecification].[color_ID] " +
                                                    "WHERE [tblItemSpecification].[item_ID] = @item_ID " +
                                                    "   AND [tblItemSpecification].[size] " + itemSize + " " +
                                                    "   AND [tblColor].[color_name] " + itemColor + " " +
                                                    "   AND [tblItemSpecification].[ismale] " + itemIsMaleValue;

                SqlCommand cmd_getItemSpecificationID = new SqlCommand(str_getItemSpecificationID, conn);
                cmd_getItemSpecificationID.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                SqlDataAdapter adp_getItemSpecificationID = new SqlDataAdapter(cmd_getItemSpecificationID);
                DataTable dt_getItemSpecificationID = new DataTable();
                adp_getItemSpecificationID.Fill(dt_getItemSpecificationID);

                if (dt_getItemSpecificationID.Rows.Count > 0)
                    return Convert.ToInt32(dt_getItemSpecificationID.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private int GetItemRemaining(int itemSpecificationID)
        {
            try
            {
                string str_getItemRemaining = "SELECT [dbo].[GetItemRemaining](@item_specification_ID, GETDATE())";

                SqlCommand cmd_getItemRemaining = new SqlCommand(str_getItemRemaining, conn);
                cmd_getItemRemaining.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                SqlDataAdapter adp_getItemRemaining = new SqlDataAdapter(cmd_getItemRemaining);
                DataTable dt_getItemRemaining = new DataTable();
                adp_getItemRemaining.Fill(dt_getItemRemaining);

                if (dt_getItemRemaining.Rows.Count > 0)
                    return Convert.ToInt32(dt_getItemRemaining.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private int GetItemPrice(int itemSpecificationID)
        {
            try
            {
                string str_getItemPrice = "SELECT TOP(1) [price] " +
                                          "FROM [tblItemPrice] " +
                                          "WHERE [item_specification_ID] = @item_specification_ID " +
                                          "ORDER BY [price_date] DESC";

                SqlCommand cmd_getItemPrice = new SqlCommand(str_getItemPrice, conn);
                cmd_getItemPrice.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                SqlDataAdapter adp_getItemPrice = new SqlDataAdapter(cmd_getItemPrice);
                DataTable dt_getItemPrice = new DataTable();
                adp_getItemPrice.Fill(dt_getItemPrice);

                if (dt_getItemPrice.Rows.Count > 0)
                    return Convert.ToInt32(dt_getItemPrice.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private string Config(ComboBox cmb)
        {
            try
            {
                if (cmb.Items.Count == 1 && cmb.Text == "")
                {
                    cmb.DataSource = null;
                    cmb.Enabled = false;
                }
                else if (cmb.Items.Count >= 1)
                    cmb.Enabled = true;
                else
                    cmb.Enabled = false;


                return cmb.Text;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        private void GetItemSizes(ComboBox cmb, int itemID)
        {
            try
            {
                string str_getItemSize = "SELECT DISTINCT CASE WHEN [size] IS NULL THEN N'' ELSE [size] END AS [itemSize] " +
                                         "FROM [tblItemSpecification] " +
                                         "WHERE [item_ID] = @item_ID " +
                                         "ORDER BY [itemSize]";

                SqlCommand cmd_getItemSize = new SqlCommand(str_getItemSize, conn);
                cmd_getItemSize.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                SqlDataAdapter adp_getItemSize = new SqlDataAdapter(cmd_getItemSize);
                DataTable dt_getItemSize = new DataTable();
                adp_getItemSize.Fill(dt_getItemSize);

                if (dt_getItemSize.Rows.Count > 0)
                {
                    cmb.ValueMember = "item_ID";
                    cmb.DisplayMember = "itemSize";
                    cmb.DataSource = dt_getItemSize;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void GetItemColors(ComboBox cmb, int itemID, string itemSize)
        {
            try
            {
                itemSize = itemSize == "" ? "IS NULL" : "= N'" + itemSize + "'";

                string str_getItemSize = "SELECT DISTINCT CASE WHEN [tblColor].[color_name] IS NULL THEN N'' ELSE [tblColor].[color_name] END AS [itemColor] " +
                                         "FROM [tblColor] RIGHT JOIN [tblItemSpecification] ON [tblColor].[color_ID] = [tblItemSpecification].[color_ID] " +
                                         "WHERE [tblItemSpecification].[item_ID] = @item_ID " +
                                         "  AND [tblItemSpecification].[size] " + itemSize + " " +
                                         "ORDER BY [itemColor]";

                SqlCommand cmd_getItemColor = new SqlCommand(str_getItemSize, conn);
                cmd_getItemColor.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                SqlDataAdapter adp_getItemColor = new SqlDataAdapter(cmd_getItemColor);
                DataTable dt_getItemColor = new DataTable();
                adp_getItemColor.Fill(dt_getItemColor);

                if (dt_getItemColor.Rows.Count > 0)
                {
                    cmb.ValueMember = "item_ID";
                    cmb.DisplayMember = "itemColor";
                    cmb.DataSource = dt_getItemColor;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void GetItemIsMaleValues(ComboBox cmb, int itemID, string itemSize, string itemColor)
        {
            try
            {
                itemSize = itemSize == "" ? "IS NULL" : "= N'" + itemSize + "'";
                itemColor = itemColor == "" ? "IS NULL" : "= N'" + itemColor + "'";

                string str_getItemIsMaleValues = "SELECT DISTINCT CASE WHEN [tblItemSpecification].[ismale] = 1 THEN N'آقایان' " +
                                                                      "WHEN [tblItemSpecification].[ismale] = 0 THEN N'بانوان' " +
                                                                      "ELSE N'' " +
                                                                 "END AS [itemIsMaleValue] " +
                                                 "FROM [tblColor] " +
                                                 "RIGHT JOIN [tblItemSpecification] ON [tblColor].[color_ID] = [tblItemSpecification].[color_ID] " +
                                                 "WHERE [tblItemSpecification].[item_ID] = @item_ID " +
                                                 "  AND [tblItemSpecification].[size] " + itemSize + " " +
                                                 "  AND [tblColor].[color_name] " + itemColor + " " +
                                                 "ORDER BY [itemIsMaleValue]";

                SqlCommand cmd_getItemIsMaleValues = new SqlCommand(str_getItemIsMaleValues, conn);
                cmd_getItemIsMaleValues.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                SqlDataAdapter adp_getItemIsMaleValues = new SqlDataAdapter(cmd_getItemIsMaleValues);
                DataTable dt_getItemIsMaleValues = new DataTable();
                adp_getItemIsMaleValues.Fill(dt_getItemIsMaleValues);

                if (dt_getItemIsMaleValues.Rows.Count > 0)
                {
                    cmb.ValueMember = "item_ID";
                    cmb.DisplayMember = "itemIsMaleValue";
                    cmb.DataSource = dt_getItemIsMaleValues;
                }
                else
                {
                    cmb.DataSource = null;
                    cmb.ValueMember = cmb.DisplayMember = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private int GetItemRowIndex(DataGridView dgv, int itemSpecificationID)
        {
            try
            {
                for (int i = 0; i < dgv.RowCount; i++)
                    if (itemSpecificationID == (int)dgv["itemSpecificationID", i].Value)
                        return i;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private int GetItemNumInShoppingBag(int itemSpecificationID)
        {
            try
            {
                int rowIndex = GetItemRowIndex(dgvStoreSellShoppingBag, itemSpecificationID);

                if (rowIndex != -1)
                    return Convert.ToInt32(dgvStoreSellShoppingBag["itemNum", rowIndex].Value);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return 0;
        }

        private int GetStoreSellTotalPrice()
        {
            try
            {
                int totalPrice = 0;
                for (int i = 0; i < dgvStoreSellShoppingBag.RowCount; i++)
                    totalPrice += Convert.ToInt32(dgvStoreSellShoppingBag["itemPrice", i].Value);

                return totalPrice;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private bool CheckExistance(ComboBox cmb, string str)
        {
            try
            {
                foreach (DataRowView row in cmb.Items)
                    if (row[cmb.DisplayMember].ToString() == str)
                        return true;

                return false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return false;
        }

        private AutoCompleteStringCollection GetAutoCompleteStringCollection(SqlCommand cmd)
        {
            try
            {
                AutoCompleteStringCollection acsc = new AutoCompleteStringCollection();

                string[] s = GetStringArray(cmd);
                if (s != null)
                    acsc.AddRange(s);

                return acsc;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        private string[] GetStringArray(SqlCommand cmd)
        {
            try
            {
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    List<string> list = new List<string>();

                    for (int i = 0; i < dt.Rows.Count; i++)
                        list.Add(dt.Rows[i][0].ToString());

                    return list.ToArray();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        public static void SetSideAthleteDetail(object athleteID, PictureBox pic, Label id, Label fullName, Label cellphone, Label rank, Label credit, bool storeCredit = false)
        {
            try
            {
                string creditFunction = "GetAthleteCredit";

                if (storeCredit)
                    creditFunction = "GetAthleteStoreCredit";

                string str_getSideAthleteDetail = "SELECT [tblAthlete].[athlete_ID], [tblAthlete].[ismale], [tblName].[name_title] + ' ' + [tblAthlete].[l_name] AS [fullName], " +
                                                  "        CASE WHEN [tblAthlete].[cellphone] IS NULL THEN N'ثبت نشده' ELSE [tblAthlete].[cellphone] END AS [phoneNumber], " +
                                                  "        [tblAthletePicture].[picture_data], (SELECT [rankName] FROM [dbo].[GetAthleteRankIDNameDate](@athlete_ID, NULL)) AS [rankName], " +
                                                  "        [dbo].[" + creditFunction + "](@athlete_ID) AS [credit] " +
                                                  "FROM [tblName] " +
                                                  "INNER JOIN [tblAthlete] ON [tblName].[name_ID] = [tblAthlete].[name_ID] " +
                                                  "LEFT JOIN [tblAthletePicture] ON [tblAthlete].[athlete_ID] = [tblAthletePicture].[athlete_ID] " +
                                                  "WHERE [tblAthlete].[athlete_ID] = @athlete_ID";

                SqlCommand cmd_getSideAthleteDetail = new SqlCommand(str_getSideAthleteDetail, conn);
                cmd_getSideAthleteDetail.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                SqlDataAdapter adp_getSideAthleteDetail = new SqlDataAdapter(cmd_getSideAthleteDetail);
                DataTable dt_getSideAthleteDetail = new DataTable();
                adp_getSideAthleteDetail.Fill(dt_getSideAthleteDetail);

                if (dt_getSideAthleteDetail.Rows.Count > 0)
                {
                    if (Convert.IsDBNull(dt_getSideAthleteDetail.Rows[0]["picture_data"]))
                    {
                        object isMale = dt_getSideAthleteDetail.Rows[0]["ismale"];

                        if (Convert.IsDBNull(isMale) || Convert.ToBoolean(isMale))
                            pic.Image = Properties.Resources.man__2_;
                        else
                            pic.Image = Properties.Resources.woman__2_;
                    }
                    else
                        pic.Image = GetImage(dt_getSideAthleteDetail.Rows[0]["picture_data"]);

                    id.Text = dt_getSideAthleteDetail.Rows[0]["athlete_ID"].ToString();
                    fullName.Text = dt_getSideAthleteDetail.Rows[0]["fullName"].ToString();
                    cellphone.Text = dt_getSideAthleteDetail.Rows[0]["phoneNumber"].ToString();
                    rank.Text = dt_getSideAthleteDetail.Rows[0]["rankName"].ToString();
                    credit.Text = dt_getSideAthleteDetail.Rows[0]["credit"].ToString();

                    GetRankColor(rank);
                    getCreditColor(credit);
                }
                else
                {
                    pic.Image = Properties.Resources.background_text;
                    id.Text = "کد";
                    fullName.Text = "نام و نام خانوادگی";
                    cellphone.Text = "شماره موبایل";

                    rank.Text = "سطح";
                    rank.BackColor = Color.LightGray;
                    rank.ForeColor = SystemColors.ControlText;

                    credit.Text = "اعتبار";
                    credit.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void BuildItemRegisterMessage(StringBuilder sb, int rowNum, string itemName, string itemSize, string itemColor, string itemIsMaleValue)
        {
            try
            {
                sb.Append("  سطر");
                sb.Append(rowNum + 1);
                sb.Append(" --> ");

                sb.Append(itemName);
                sb.Append(" --- ");
                sb.Append(itemSize == "" ? "ث.ن" : itemSize);
                sb.Append(" - ");
                sb.Append(itemColor == "" ? "ث.ن" : itemColor);
                sb.Append(" - ");
                sb.AppendLine(itemIsMaleValue == "" ? "ث.ن" : itemIsMaleValue);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Main Codes --------------------

        private void cmbStoreItemSizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreSellItemSizes.Text;

                    GetItemColors(cmbStoreSellItemColors, (int)cmbStoreSellItemsName.SelectedValue, cmbStoreSellItemSizes.Text);
                    string itemColor = Config(cmbStoreSellItemColors);

                    GetItemIsMaleValues(cmbStoreSellItemIsMaleValues, (int)cmbStoreSellItemsName.SelectedValue, cmbStoreSellItemSizes.Text, cmbStoreSellItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreSellItemIsMaleValues);

                    itemSpecificationID = GetItemSpecificationID((int)cmbStoreSellItemsName.SelectedValue, itemSize, itemColor, itemIsMaleValue);

                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();
                    lblStoreSellItemPrice.Text = GetItemPrice(itemSpecificationID).ToString();

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreItemColors_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreSellItemSizes.Text;
                    string itemColor = cmbStoreSellItemColors.Text;

                    GetItemIsMaleValues(cmbStoreSellItemIsMaleValues, (int)cmbStoreSellItemsName.SelectedValue, cmbStoreSellItemSizes.Text, cmbStoreSellItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreSellItemIsMaleValues);

                    itemSpecificationID = GetItemSpecificationID((int)cmbStoreSellItemsName.SelectedValue, itemSize, itemColor, itemIsMaleValue);

                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();
                    lblStoreSellItemPrice.Text = GetItemPrice(itemSpecificationID).ToString();

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreItemIsMaleValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreSellItemSizes.Text;
                    string itemColor = cmbStoreSellItemColors.Text;
                    string itemIsMaleValue = cmbStoreSellItemIsMaleValues.Text;

                    itemSpecificationID = GetItemSpecificationID((int)cmbStoreSellItemsName.SelectedValue, itemSize, itemColor, itemIsMaleValue);

                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();
                    lblStoreSellItemPrice.Text = GetItemPrice(itemSpecificationID).ToString();

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreItemsNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    GetItemSizes(cmbStoreSellItemSizes, (int)cmbStoreSellItemsName.SelectedValue);
                    string itemSize = Config(cmbStoreSellItemSizes);

                    GetItemColors(cmbStoreSellItemColors, (int)cmbStoreSellItemsName.SelectedValue, cmbStoreSellItemSizes.Text);
                    string itemColor = Config(cmbStoreSellItemColors);

                    GetItemIsMaleValues(cmbStoreSellItemIsMaleValues, (int)cmbStoreSellItemsName.SelectedValue, cmbStoreSellItemSizes.Text, cmbStoreSellItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreSellItemIsMaleValues);

                    itemSpecificationID = GetItemSpecificationID((int)cmbStoreSellItemsName.SelectedValue, itemSize, itemColor, itemIsMaleValue);

                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();
                    lblStoreSellItemPrice.Text = GetItemPrice(itemSpecificationID).ToString();

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStorePurchase_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbStoreSellItemsName.Text.Trim() == "")
                    MessageBox.Show("کالایی انتخاب نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (!CheckExistance(cmbStoreSellItemsName, RemoveExtraWhiteSpaces(cmbStoreSellItemsName.Text)))
                    MessageBox.Show("کالا نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (Convert.ToInt32(lblStoreSellItemRemaining.Text) == 0)
                    MessageBox.Show("موجودی کالای مورد نظر صفر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (nudStoreSellItemNumber.Value == 0)
                    MessageBox.Show("تعداد درخواستی صفر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (nudStoreSellItemNumber.Value > Convert.ToInt32(lblStoreSellItemRemaining.Text))
                    MessageBox.Show("تعداد کالای درخواستی از موجودی بیشتر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else
                {
                    int dgvRowIndex = GetItemRowIndex(dgvStoreSellShoppingBag, itemSpecificationID);

                    if (dgvRowIndex != -1)
                    {
                        dgvStoreSellShoppingBag["itemNum", dgvRowIndex].Value = Convert.ToInt32(dgvStoreSellShoppingBag["itemNum", dgvRowIndex].Value) + nudStoreSellItemNumber.Value;
                        dgvStoreSellShoppingBag["itemPrice", dgvRowIndex].Value = Convert.ToInt32(dgvStoreSellShoppingBag["itemPrice", dgvRowIndex].Value) + (Convert.ToInt32(lblStoreSellItemPrice.Text) * nudStoreSellItemNumber.Value);
                    }
                    else
                        dgvStoreSellShoppingBag.Rows.Add(itemSpecificationID, cmbStoreSellItemsName.Text, cmbStoreSellItemSizes.Text, cmbStoreSellItemColors.Text, cmbStoreSellItemIsMaleValues.Text, nudStoreSellItemNumber.Value, Convert.ToInt32(lblStoreSellItemPrice.Text) * nudStoreSellItemNumber.Value);

                    lblStoreSellItemsTotalPrice.Text = GetStoreSellTotalPrice().ToString();
                    lblStoreSellItemRemaining.Text = (GetItemRemaining(itemSpecificationID) - GetItemNumInShoppingBag(itemSpecificationID)).ToString();

                    nudStoreSellItemNumber.Value = 0;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreWriteInFactor_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvStoreSellShoppingBag.RowCount == 0)
                    MessageBox.Show("سبد خرید شما خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (cmbStoreSellAthletesName.Text.Trim() == "")
                    MessageBox.Show("هنرجویی انتخاب نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (!CheckExistance(cmbStoreSellAthletesName, RemoveExtraWhiteSpaces(cmbStoreSellAthletesName.Text)))
                    MessageBox.Show("هنرجو نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else
                {
                    btnPays_Click(sender, e);
                    txtPayType.Text = "فروشگاه";

                    cmbPayAthlete.SelectedValue = cmbStoreSellAthletesName.SelectedValue;
                    cmbPayType.SelectedIndex = 1;

                    lblPayTotalPrice.Text = lblStoreSellItemsTotalPrice.Text;
                    txtPayAmount.Text = lblStoreSellItemsTotalPrice.Text;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreAthletesName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbStoreSellAthletesName.SelectedValue != null)
                    SetSideAthleteDetail(cmbStoreSellAthletesName.SelectedValue, pcbStoreSellAthletePic, lblStoreSellAthleteID, lblStoreSellAthleteName, lblStoreSellAthleteCellphone, lblStoreSellAthleteRank, lblStoreSellAthleteCredit, true);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreSellResetShoppingBagTable_Click(object sender, EventArgs e)
        {
            try
            {
                cmbStoreSellAthletesName.SelectedValue = -1;
                cmbStoreSellAthletesName.Text = "انتخاب هنرجو";

                limitRedoing = true;

                ResetStorePage();

                cmbStoreSellItemsName.SelectedIndex = -1;
                cmbStoreSellItemsName.Text = "انتخاب کالا";

                limitRedoing = false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreManageResetItemRegisterTable_Click(object sender, EventArgs e)
        {
            try
            {
                dgvStoreManageItemRegister.Rows.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreManageItemRegister_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvStoreManageItemRegister.RowCount == 1)
                    MessageBox.Show("کالایی برای ثبت یافت نشد", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else
                {
                    dgvStoreManageItemRegister.EndEdit();
                    dgvStoreManageItemRegister.ClearSelection();

                    if (dgvItemRegisterErrorRows.Count > 0)
                    {
                        for (int i = 0; i < dgvItemRegisterErrorRows.Count; i++)
                        {
                            if (dgvItemRegisterErrorRows[i] % 2 == 0)
                            {
                                dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.BackColor = Color.FromArgb(224, 224, 224);
                                dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 224, 224);
                            }
                            else
                            {
                                dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.BackColor = Color.LightGray;
                                dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.SelectionBackColor = Color.LightGray;
                            }

                            dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.ForeColor = SystemColors.ControlText;
                            dgvStoreManageItemRegister.Rows[dgvItemRegisterErrorRows[i]].DefaultCellStyle.SelectionForeColor = SystemColors.ControlText;
                        }
                    }

                    dgvItemRegisterErrorRows = new List<int>();

                    for (int i = 0; i < dgvStoreManageItemRegister.RowCount - 1; i++)
                    {
                        if (dgvStoreManageItemRegister["itemName", i].Value == null ||
                            dgvStoreManageItemRegister["itemNumber", i].Value == null ||
                            dgvStoreManageItemRegister["itemPrice", i].Value == null ||
                            dgvStoreManageItemRegister["itemName", i].Value.ToString().Trim() == "")
                        {
                            dgvStoreManageItemRegister.Rows[i].DefaultCellStyle.BackColor = Color.RosyBrown;
                            dgvStoreManageItemRegister.Rows[i].DefaultCellStyle.SelectionBackColor = Color.RosyBrown;

                            dgvStoreManageItemRegister.Rows[i].DefaultCellStyle.ForeColor = Color.White;
                            dgvStoreManageItemRegister.Rows[i].DefaultCellStyle.SelectionForeColor = Color.White;

                            dgvItemRegisterErrorRows.Add(i);
                        }
                    }

                    if (dgvItemRegisterErrorRows.Count == 0)
                    {
                        DateTime now = DateTime.Now;

                        string str_newIncomeFactor = "INSERT INTO [tblIncomeFactor] " +
                                                     "VALUES (@factor_date)";

                        SqlCommand cmd_newIncomeFactor = new SqlCommand(str_newIncomeFactor, conn);
                        cmd_newIncomeFactor.Parameters.Add("@factor_date", SqlDbType.DateTime2).Value = now;

                        conn.Open();
                        cmd_newIncomeFactor.ExecuteNonQuery();

                        string str_getMaxIncomeFactorID = "SELECT IDENT_CURRENT('tblIncomeFactor')";

                        SqlCommand cmd_getMaxIncomeFactorID = new SqlCommand(str_getMaxIncomeFactorID, conn);

                        SqlDataAdapter adp_getMaxIncomeFactorID = new SqlDataAdapter(cmd_getMaxIncomeFactorID);
                        DataTable dt_getMaxIncomeFactorID = new DataTable();
                        adp_getMaxIncomeFactorID.Fill(dt_getMaxIncomeFactorID);

                        bool isRegisteringItemsEnded = false;

                        string itemName, itemColor = "", itemIsMaleValue = "";
                        object itemSize, itemColorID, itemIsMaleState;
                        int itemID, itemSpecificationID;

                        StringBuilder repetitiveItems = new StringBuilder(), newItems = new StringBuilder();

                        int i = 0;

                        while (!isRegisteringItemsEnded)
                        {
                            itemName = dgvStoreManageItemRegister["itemName", i].Value.ToString();
                            itemName = RemoveExtraWhiteSpaces(itemName);

                            itemID = GetItemID(itemName);

                            if (itemID == -1)
                            {
                                itemID = GetMaxItemID();

                                string str_newItem = "INSERT INTO [tblItem]" +
                                                        "VALUES (@item_name)";

                                SqlCommand cmd_newItem = new SqlCommand(str_newItem, conn);
                                cmd_newItem.Parameters.Add("@item_name", SqlDbType.NVarChar).Value = itemName;

                                cmd_newItem.ExecuteNonQuery();
                            }

                            do
                            {
                                if (dgvStoreManageItemRegister["itemSize", i].Value != null)
                                {
                                    itemSize = dgvStoreManageItemRegister["itemSize", i].Value.ToString();
                                    itemSize = RemoveExtraWhiteSpaces(itemSize.ToString());

                                    if (itemSize.ToString() == "")
                                        itemSize = DBNull.Value;
                                }
                                else
                                    itemSize = DBNull.Value;

                                if (dgvStoreManageItemRegister["itemColor", i].Value != null)
                                {
                                    itemColor = dgvStoreManageItemRegister["itemColor", i].Value.ToString();
                                    itemColor = RemoveExtraWhiteSpaces(itemColor);

                                    if (itemColor != "")
                                        itemColorID = GetColorID(itemColor);
                                    else
                                        itemColorID = DBNull.Value;
                                }
                                else
                                {
                                    itemColor = "";
                                    itemColorID = DBNull.Value;
                                }

                                if (dgvStoreManageItemRegister["itemIsMaleValue", i].Value != null)
                                {
                                    itemIsMaleValue = dgvStoreManageItemRegister["itemIsMaleValue", i].Value.ToString();
                                    itemIsMaleState = itemIsMaleValue == "آقایان" ? true : false;
                                }
                                else
                                {
                                    itemIsMaleValue = "";
                                    itemIsMaleState = DBNull.Value;
                                }

                                itemSpecificationID = GetItemSpecificationID(itemID, itemSize.ToString(), itemColor, itemIsMaleValue);

                                if (itemSpecificationID == -1)
                                {
                                    string str_newItemSpecification = "INSERT INTO [tblItemSpecification] " +
                                                                        "VALUES (@item_ID, @color_ID, @size, @ismale)";

                                    SqlCommand cmd_newItemSpecification = new SqlCommand(str_newItemSpecification, conn);
                                    cmd_newItemSpecification.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;
                                    cmd_newItemSpecification.Parameters.Add("@size", SqlDbType.NVarChar).Value = itemSize;
                                    cmd_newItemSpecification.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = itemColorID;
                                    cmd_newItemSpecification.Parameters.Add("@ismale", SqlDbType.Bit).Value = itemIsMaleState;

                                    cmd_newItemSpecification.ExecuteNonQuery();

                                    string str_getMaxItemSpecificationID = "SELECT IDENT_CURRENT('tblItemSpecification')";

                                    SqlCommand cmd_getMaxItemSpecificationID = new SqlCommand(str_getMaxItemSpecificationID, conn);

                                    SqlDataAdapter adp_getMaxItemSpecificationID = new SqlDataAdapter(cmd_getMaxItemSpecificationID);
                                    DataTable dt_getMaxItemSpecificationID = new DataTable();
                                    adp_getMaxItemSpecificationID.Fill(dt_getMaxItemSpecificationID);

                                    itemSpecificationID = Convert.ToInt32(dt_getMaxItemSpecificationID.Rows[0][0]);

                                    string str_newItemPrice = "INSERT INTO [tblItemPrice] " +
                                                              "VALUES (@item_specification_ID, @price, @price_date)";

                                    SqlCommand cmd_newItemPrice = new SqlCommand(str_newItemPrice, conn);
                                    cmd_newItemPrice.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;
                                    cmd_newItemPrice.Parameters.Add("@price", SqlDbType.Int).Value = dgvStoreManageItemRegister["itemPrice", i].Value;
                                    cmd_newItemPrice.Parameters.Add("@price_date", SqlDbType.DateTime2).Value = now.Date;

                                    cmd_newItemPrice.ExecuteNonQuery();

                                    BuildItemRegisterMessage(newItems, i, itemName, itemSize.ToString(), itemColor, itemIsMaleValue);
                                }
                                else
                                    BuildItemRegisterMessage(repetitiveItems, i, itemName, itemSize.ToString(), itemColor, itemIsMaleValue);

                                string str_newIncomeOperation = "INSERT INTO [tblIncomeOperation] " +
                                                                "VALUES (@income_factor_ID, @item_specification_ID, @number)";

                                SqlCommand cmd_newIncomeOperation = new SqlCommand(str_newIncomeOperation, conn);
                                cmd_newIncomeOperation.Parameters.Add("@income_factor_ID", SqlDbType.Int).Value = dt_getMaxIncomeFactorID.Rows[0][0];
                                cmd_newIncomeOperation.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;
                                cmd_newIncomeOperation.Parameters.Add("@number", SqlDbType.SmallInt).Value = dgvStoreManageItemRegister["itemNumber", i].Value;

                                cmd_newIncomeOperation.ExecuteNonQuery();

                                if (i == dgvStoreManageItemRegister.RowCount - 2)
                                {
                                    isRegisteringItemsEnded = true;
                                    break;
                                }

                            } while (dgvStoreManageItemRegister["itemName", ++i].Value.ToString().Trim() == itemName);
                        }

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("عملیات موفق\n");

                        if (newItems.Length > 0)
                        {
                            sb.AppendLine(":کالاهای جدید");
                            sb.AppendLine(newItems.ToString());
                        }
                        if (repetitiveItems.Length > 0)
                        {
                            sb.AppendLine(":کالاهای تکراری که موجودیشان افزایش یافت");
                            sb.AppendLine(repetitiveItems.ToString());
                        }

                        MessageBox.Show(sb.ToString(), "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        InitializeStoreManagementPage();

                        conn.Close();
                    }
                }
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemsNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    GetItemSizes(cmbStoreManageItemSizes, (int)cmbStoreManageItemsNames.SelectedValue);
                    string itemSize = Config(cmbStoreManageItemSizes);

                    GetItemColors(cmbStoreManageItemColors, (int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text);
                    string itemColor = Config(cmbStoreManageItemColors);

                    GetItemIsMaleValues(cmbStoreManageItemIsMaleValues, (int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text, cmbStoreManageItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreManageItemIsMaleValues);

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemSizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreManageItemSizes.Text;

                    GetItemColors(cmbStoreManageItemColors, (int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text);
                    string itemColor = Config(cmbStoreManageItemColors);

                    GetItemIsMaleValues(cmbStoreManageItemIsMaleValues, (int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text, cmbStoreManageItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreManageItemIsMaleValues);

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemColors_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreManageItemSizes.Text;
                    string itemColor = cmbStoreManageItemColors.Text;

                    GetItemIsMaleValues(cmbStoreManageItemIsMaleValues, (int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text, cmbStoreManageItemColors.Text);
                    string itemIsMaleValue = Config(cmbStoreManageItemIsMaleValues);

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbStoreManageItemIsMaleValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!limitRedoing)
                {
                    limitRedoing = true;

                    string itemSize = cmbStoreManageItemSizes.Text;
                    string itemColor = cmbStoreManageItemColors.Text;
                    string itemIsMaleValue = cmbStoreManageItemIsMaleValues.Text;

                    limitRedoing = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreManageEditItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStoreManageEditItem.Text == "ویرایش")
                {
                    string itemName = RemoveExtraWhiteSpaces(cmbStoreManageItemsNames.Text);

                    if (GetItemID(itemName) == -1)
                    {
                        limitRedoing = true;
                        ResetItemDetails(cmbStoreManageItemSizes, cmbStoreManageItemColors, cmbStoreManageItemIsMaleValues);
                        limitRedoing = false;

                        MessageBox.Show("کالایی با این نام موجود نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                    else
                    {
                        string str_getItems = "SELECT [item_name] " +
                                              "FROM [tblItem] " +
                                              "ORDER BY [item_name]";

                        txtStoreManageItemsNames.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getItems, conn));

                        string str_getItemsSizes = "SELECT DISTINCT [size] " +
                                                   "FROM [tblItemSpecification] " +
                                                   "WHERE [size] IS NOT NULL " +
                                                   "ORDER BY [size]";

                        txtStoreManageItemSizes.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getItemsSizes, conn));

                        string str_getitemsColors = "SELECT [color_name] " +
                                                    "FROM [tblColor] " +
                                                    "ORDER BY [color_name]";

                        txtStoreManageItemColors.AutoCompleteCustomSource = GetAutoCompleteStringCollection(new SqlCommand(str_getitemsColors, conn));

                        itemSpecificationID = GetItemSpecificationID((int)cmbStoreManageItemsNames.SelectedValue, cmbStoreManageItemSizes.Text, cmbStoreManageItemColors.Text, cmbStoreManageItemIsMaleValues.Text);
                        txtStoreManageItemPrice.Text = itemPrice_old = GetItemPrice(itemSpecificationID).ToString();

                        txtStoreManageItemsNames.Text = cmbStoreManageItemsNames.Text;
                        txtStoreManageItemSizes.Text = cmbStoreManageItemSizes.Text;
                        txtStoreManageItemColors.Text = cmbStoreManageItemColors.Text;
                        cmbStoreManageItemDefIsMaleValues.Text = cmbStoreManageItemIsMaleValues.Text == "" ? "هر دو" : cmbStoreManageItemIsMaleValues.Text;

                        cmbStoreManageItemsNames.Visible = cmbStoreManageItemSizes.Visible = cmbStoreManageItemColors.Visible = cmbStoreManageItemIsMaleValues.Visible = false;
                        txtStoreManageItemsNames.Visible = txtStoreManageItemSizes.Visible = txtStoreManageItemColors.Visible = cmbStoreManageItemDefIsMaleValues.Visible = true;
                        lblStoreManageItemPrice.Visible = txtStoreManageItemPrice.Visible = true;

                        btnStoreManageDeleteItem.Text = "بازگشت";
                        btnStoreManageEditItem.Text = "بروزرسانی";
                    }
                }
                else
                {
                    int itemID_old = (int)cmbStoreManageItemsNames.SelectedValue, itemID;
                    string itemName, itemColor, itemIsMaleValue;
                    object itemSize, itemColorID, itemIsMaleState;

                    itemName = RemoveExtraWhiteSpaces(txtStoreManageItemsNames.Text);
                    itemID = GetItemID(itemName);

                    itemSize = RemoveExtraWhiteSpaces(txtStoreManageItemSizes.Text);
                    if (itemSize.ToString() == "")
                        itemSize = DBNull.Value;

                    itemColor = RemoveExtraWhiteSpaces(txtStoreManageItemColors.Text);
                    itemIsMaleValue = cmbStoreManageItemDefIsMaleValues.Text;

                    if (itemID == -1 || itemID == itemID_old)
                    {
                        int itemSpecificationID_new;

                        if (itemID == -1)
                            itemSpecificationID_new = -1;
                        else
                        {
                            itemSpecificationID_new = GetItemSpecificationID(itemID, itemSize.ToString(), itemColor, itemIsMaleValue);

                            if (itemSpecificationID_new != -1 && txtStoreManageItemPrice.Text == itemPrice_old)
                            {
                                MessageBox.Show("کالایی با این مشخصات در انبار موجود است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                return;
                            }
                        }

                        conn.Open();

                        if (itemID == -1)
                        {
                            string str_newItem = "INSERT INTO [tblItem] " +
                                                 "VALUES (@item_name)";

                            SqlCommand cmd_newItem = new SqlCommand(str_newItem, conn);
                            cmd_newItem.Parameters.Add("@item_name", SqlDbType.NVarChar).Value = itemName;

                            cmd_newItem.ExecuteNonQuery();

                            string str_getMaxitemID = "SELECT IDENT_CURRENT('tblItem')";

                            SqlCommand cmd_getMaxitemID = new SqlCommand(str_getMaxitemID, conn);

                            SqlDataAdapter adp_getMaxitemID = new SqlDataAdapter(cmd_getMaxitemID);
                            DataTable dt_getMaxitemID = new DataTable();
                            adp_getMaxitemID.Fill(dt_getMaxitemID);

                            itemColorID = itemColor == "" ? DBNull.Value : (object)GetColorID(itemColor);

                            if (itemIsMaleValue == "آقایان")
                                itemIsMaleState = true;
                            else if (itemIsMaleValue == "بانوان")
                                itemIsMaleState = false;
                            else
                                itemIsMaleState = DBNull.Value;

                            string str_getItemColorID = "SELECT [color_ID]" +
                                                        "FROM [tblItemSpecification] " +
                                                        "WHERE [item_specification_ID] = @item_specification_ID";

                            SqlCommand cmd_getItemColorID = new SqlCommand(str_getItemColorID, conn);
                            cmd_getItemColorID.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                            SqlDataAdapter adp_getItemColorID = new SqlDataAdapter(cmd_getItemColorID);
                            DataTable dt_getItemColorID = new DataTable();
                            adp_getItemColorID.Fill(dt_getItemColorID);

                            if (!Convert.IsDBNull(dt_getItemColorID.Rows[0][0]))
                            {
                                string str_getColorIDNum = "SELECT COUNT([item_specification_ID]) " +
                                                           "FROM [tblItemSpecification] " +
                                                           "WHERE [color_ID] = @color_ID";

                                SqlCommand cmd_getColorIDNum = new SqlCommand(str_getColorIDNum, conn);
                                cmd_getColorIDNum.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                                SqlDataAdapter adp_getColorIDNum = new SqlDataAdapter(cmd_getColorIDNum);
                                DataTable dt_getColorIDNum = new DataTable();
                                adp_getColorIDNum.Fill(dt_getColorIDNum);

                                if ((int)dt_getColorIDNum.Rows[0][0] == 1)
                                {
                                    string str_deleteColor = "DELETE FROM [tblColor] " +
                                                             "WHERE [color_ID] = @color_ID";

                                    SqlCommand cmd_deleteColor = new SqlCommand(str_deleteColor, conn);
                                    cmd_deleteColor.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                                    cmd_deleteColor.ExecuteNonQuery();
                                }
                            }

                            string str_updateItemSpecification = "UPDATE [tblItemSpecification] " +
                                                                    "SET [item_ID] = @item_ID, [size] = @size, " +
                                                                    "    [color_ID] = @color_ID, [ismale] = @ismale " +
                                                                    "WHERE [item_specification_ID] = @item_specification_ID";

                            SqlCommand cmd_updateItemSpecification = new SqlCommand(str_updateItemSpecification, conn);
                            cmd_updateItemSpecification.Parameters.Add("@item_ID", SqlDbType.Int).Value = dt_getMaxitemID.Rows[0][0];
                            cmd_updateItemSpecification.Parameters.Add("@size", SqlDbType.NVarChar).Value = itemSize;
                            cmd_updateItemSpecification.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = itemColorID;
                            cmd_updateItemSpecification.Parameters.Add("@ismale", SqlDbType.Bit).Value = itemIsMaleState;
                            cmd_updateItemSpecification.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                            cmd_updateItemSpecification.ExecuteNonQuery();

                            string str_getOldItemNum = "SELECT COUNT([item_specification_ID]) " +
                                                        "FROM [tblItemSpecification] " +
                                                        "WHERE [item_ID] = @item_ID";

                            SqlCommand cmd_getOldItemNum = new SqlCommand(str_getOldItemNum, conn);
                            cmd_getOldItemNum.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID_old;

                            SqlDataAdapter adp_getOldItemNum = new SqlDataAdapter(cmd_getOldItemNum);
                            DataTable dt_getOldItemNum = new DataTable();
                            adp_getOldItemNum.Fill(dt_getOldItemNum);

                            if ((int)dt_getOldItemNum.Rows[0][0] == 0)
                            {
                                string str_deleteOldItem = "DELETE FROM [tblItem] " +
                                                            "WHERE [item_ID] = @item_ID";

                                SqlCommand cmd_deleteOldItem = new SqlCommand(str_deleteOldItem, conn);
                                cmd_deleteOldItem.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID_old;

                                cmd_deleteOldItem.ExecuteNonQuery();
                            }
                        }
                        else if (itemSpecificationID_new == -1)
                        {
                            itemColorID = itemColor == "" ? DBNull.Value : (object)GetColorID(itemColor);

                            if (itemIsMaleValue == "آقایان")
                                itemIsMaleState = true;
                            else if (itemIsMaleValue == "بانوان")
                                itemIsMaleState = false;
                            else
                                itemIsMaleState = DBNull.Value;

                            string str_updateItemSpecification = "UPDATE [tblItemSpecification] " +
                                                                 "SET [size] = @size, [color_ID] = @color_ID, [ismale] = @ismale " +
                                                                 "WHERE [item_specification_ID] = @item_specification_ID";

                            SqlCommand cmd_updateItemSpecification = new SqlCommand(str_updateItemSpecification, conn);
                            cmd_updateItemSpecification.Parameters.Add("@size", SqlDbType.NVarChar).Value = itemSize;
                            cmd_updateItemSpecification.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = itemColorID;
                            cmd_updateItemSpecification.Parameters.Add("@ismale", SqlDbType.Bit).Value = itemIsMaleState;
                            cmd_updateItemSpecification.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                            cmd_updateItemSpecification.ExecuteNonQuery();
                        }

                        if (txtStoreManageItemPrice.Text != itemPrice_old)
                        {
                            string str_updateItemPrice = "INSERT INTO [tblItemPrice] " +
                                                         "VALUES (@item_specification_ID, @price, @price_date)";

                            SqlCommand cmd_updateItemPrice = new SqlCommand(str_updateItemPrice, conn);
                            cmd_updateItemPrice.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;
                            cmd_updateItemPrice.Parameters.Add("@price", SqlDbType.Int).Value = txtStoreManageItemPrice.Text;
                            cmd_updateItemPrice.Parameters.Add("@price_date", SqlDbType.DateTime2).Value = DateTime.Now;

                            cmd_updateItemPrice.ExecuteNonQuery();
                        }

                        conn.Close();
                    }
                    else
                    {
                        int itemSpecificationID_new = GetItemSpecificationID(itemID, itemSize.ToString(), itemColor, itemIsMaleValue);

                        if (itemSpecificationID_new != -1 && txtStoreManageItemPrice.Text == itemPrice_old)
                        {
                            MessageBox.Show("کالایی با این مشخصات در انبار موجود است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                            return;
                        }

                        itemColorID = itemColor == "" ? DBNull.Value : (object)GetColorID(itemColor);

                        if (itemIsMaleValue == "آقایان")
                            itemIsMaleState = true;
                        else if (itemIsMaleValue == "بانوان")
                            itemIsMaleState = false;
                        else
                            itemIsMaleState = DBNull.Value;

                        string str_getItemColorID = "SELECT [color_ID]" +
                                                            "FROM [tblItemSpecification] " +
                                                            "WHERE [item_specification_ID] = @item_specification_ID";

                        SqlCommand cmd_getItemColorID = new SqlCommand(str_getItemColorID, conn);
                        cmd_getItemColorID.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                        SqlDataAdapter adp_getItemColorID = new SqlDataAdapter(cmd_getItemColorID);
                        DataTable dt_getItemColorID = new DataTable();
                        adp_getItemColorID.Fill(dt_getItemColorID);

                        if (!Convert.IsDBNull(dt_getItemColorID.Rows[0][0]))
                        {
                            string str_getColorIDNum = "SELECT COUNT([item_specification_ID]) " +
                                                       "FROM [tblItemSpecification] " +
                                                       "WHERE [color_ID] = @color_ID";

                            SqlCommand cmd_getColorIDNum = new SqlCommand(str_getColorIDNum, conn);
                            cmd_getColorIDNum.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                            SqlDataAdapter adp_getColorIDNum = new SqlDataAdapter(cmd_getColorIDNum);
                            DataTable dt_getColorIDNum = new DataTable();
                            adp_getColorIDNum.Fill(dt_getColorIDNum);

                            if ((int)dt_getColorIDNum.Rows[0][0] == 1)
                            {
                                string str_deleteColor = "DELETE FROM [tblColor] " +
                                                         "WHERE [color_ID] = @color_ID";

                                SqlCommand cmd_deleteColor = new SqlCommand(str_deleteColor, conn);
                                cmd_deleteColor.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                                cmd_deleteColor.ExecuteNonQuery();
                            }
                        }

                        string str_updateItemSpecification = "UPDATE [tblItemSpecification] " +
                                                             "SET [item_ID] = @item_ID, [size] = @size, " +
                                                             "    [color_ID] = @color_ID, [ismale] = @ismale " +
                                                             "WHERE [item_specification_ID] = @item_specification_ID";

                        SqlCommand cmd_updateItemSpecification = new SqlCommand(str_updateItemSpecification, conn);
                        cmd_updateItemSpecification.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;
                        cmd_updateItemSpecification.Parameters.Add("@size", SqlDbType.NVarChar).Value = itemSize;
                        cmd_updateItemSpecification.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = itemColorID;
                        cmd_updateItemSpecification.Parameters.Add("@ismale", SqlDbType.Bit).Value = itemIsMaleState;
                        cmd_updateItemSpecification.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                        conn.Open();
                        cmd_updateItemSpecification.ExecuteNonQuery();

                        string str_getOldItemNum = "SELECT COUNT([item_specification_ID]) " +
                                                   "FROM [tblItemSpecification] " +
                                                   "WHERE [item_ID] = @item_ID";

                        SqlCommand cmd_getOldItemNum = new SqlCommand(str_getOldItemNum, conn);
                        cmd_getOldItemNum.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID_old;

                        SqlDataAdapter adp_getOldItemNum = new SqlDataAdapter(cmd_getOldItemNum);
                        DataTable dt_getOldItemNum = new DataTable();
                        adp_getOldItemNum.Fill(dt_getOldItemNum);

                        if ((int)dt_getOldItemNum.Rows[0][0] == 0)
                        {
                            string str_deleteOldItem = "DELETE FROM [tblItem] " +
                                                       "WHERE [item_ID] = @item_ID";

                            SqlCommand cmd_deleteOldItem = new SqlCommand(str_deleteOldItem, conn);
                            cmd_deleteOldItem.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID_old;

                            cmd_deleteOldItem.ExecuteNonQuery();
                        }

                        if (txtStoreManageItemPrice.Text != itemPrice_old)
                        {
                            string str_updateItemPrice = "INSERT INTO [tblItemPrice] " +
                                                         "VALUES (@item_specification_ID, @price, @price_date)";

                            SqlCommand cmd_updateItemPrice = new SqlCommand(str_updateItemPrice, conn);
                            cmd_updateItemPrice.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;
                            cmd_updateItemPrice.Parameters.Add("@price", SqlDbType.Int).Value = txtStoreManageItemPrice.Text;
                            cmd_updateItemPrice.Parameters.Add("@price_date", SqlDbType.DateTime2).Value = DateTime.Now;

                            cmd_updateItemPrice.ExecuteNonQuery();
                        }

                        conn.Close();
                    }

                    MessageBox.Show("کالای مورد نظر با موفقیت بروزرسانی شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    InitializeStoreManagementManageItem();

                    txtStoreManageItemsNames.Visible = txtStoreManageItemSizes.Visible = txtStoreManageItemColors.Visible = cmbStoreManageItemDefIsMaleValues.Visible = false;
                    cmbStoreManageItemsNames.Visible = cmbStoreManageItemSizes.Visible = cmbStoreManageItemColors.Visible = cmbStoreManageItemIsMaleValues.Visible = true;
                    lblStoreManageItemPrice.Visible = txtStoreManageItemPrice.Visible = false;

                    btnStoreManageDeleteItem.Text = "حذف";
                    btnStoreManageEditItem.Text = "ویرایش";

                    cmbStoreManageItemsNames.Focus();
                }
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnStoreManageDeleteItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStoreManageDeleteItem.Text == "حذف")
                {
                    string itemName = RemoveExtraWhiteSpaces(cmbStoreManageItemsNames.Text);

                    if (GetItemID(itemName) == -1)
                    {
                        limitRedoing = true;
                        ResetItemDetails(cmbStoreManageItemSizes, cmbStoreManageItemColors, cmbStoreManageItemIsMaleValues);
                        limitRedoing = false;

                        MessageBox.Show("کالایی با این نام موجود نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                    else
                    {
                        DialogResult result = MessageBox.Show("آیا از حذف کالا اطمینان دارید؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        if (result == DialogResult.Yes)
                        {
                            int itemID = (int)cmbStoreManageItemsNames.SelectedValue;
                            itemSpecificationID = GetItemSpecificationID(itemID, cmbStoreManageItemSizes.Text, cmbStoreManageItemColors.Text, cmbStoreManageItemIsMaleValues.Text);

                            if (itemSpecificationID != -1)
                            {
                                conn.Open();

                                string str_getItemColorID = "SELECT [color_ID]" +
                                                            "FROM [tblItemSpecification] " +
                                                            "WHERE [item_specification_ID] = @item_specification_ID";

                                SqlCommand cmd_getItemColorID = new SqlCommand(str_getItemColorID, conn);
                                cmd_getItemColorID.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                                SqlDataAdapter adp_getItemColorID = new SqlDataAdapter(cmd_getItemColorID);
                                DataTable dt_getItemColorID = new DataTable();
                                adp_getItemColorID.Fill(dt_getItemColorID);

                                if (!Convert.IsDBNull(dt_getItemColorID.Rows[0][0]))
                                {
                                    string str_getColorIDNum = "SELECT COUNT([item_specification_ID]) " +
                                                               "FROM [tblItemSpecification] " +
                                                               "WHERE [color_ID] = @color_ID";

                                    SqlCommand cmd_getColorIDNum = new SqlCommand(str_getColorIDNum, conn);
                                    cmd_getColorIDNum.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                                    SqlDataAdapter adp_getColorIDNum = new SqlDataAdapter(cmd_getColorIDNum);
                                    DataTable dt_getColorIDNum = new DataTable();
                                    adp_getColorIDNum.Fill(dt_getColorIDNum);

                                    if ((int)dt_getColorIDNum.Rows[0][0] == 1)
                                    {
                                        string str_deleteColor = "DELETE FROM [tblColor] " +
                                                                 "WHERE [color_ID] = @color_ID";

                                        SqlCommand cmd_deleteColor = new SqlCommand(str_deleteColor, conn);
                                        cmd_deleteColor.Parameters.Add("@color_ID", SqlDbType.SmallInt).Value = dt_getItemColorID.Rows[0][0];

                                        cmd_deleteColor.ExecuteNonQuery();
                                    }
                                }

                                string str_getItemIncomeFactorIDs = "SELECT [income_factor_ID] " +
                                                                    "FROM [tblIncomeOperation] " +
                                                                    "WHERE [item_specification_ID] = @item_specification_ID";

                                SqlCommand cmd_getItemIncomeFactorIDs = new SqlCommand(str_getItemIncomeFactorIDs, conn);
                                cmd_getItemIncomeFactorIDs.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                                string str_getItemSellFactorIDs = "SELECT [sell_factor_ID] " +
                                                                  "FROM [tblSellOperation] " +
                                                                  "WHERE [item_specification_ID] = @item_specification_ID";

                                SqlCommand cmd_getItemSellFactorIDs = new SqlCommand(str_getItemSellFactorIDs, conn);
                                cmd_getItemSellFactorIDs.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                                string[] itemIncomeFactorIDs = GetStringArray(cmd_getItemIncomeFactorIDs);
                                string[] itemSellFactorIDs = GetStringArray(cmd_getItemSellFactorIDs);

                                string str_deleteItemSpecification = "DELETE FROM [tblItemSpecification] " +
                                                                     "WHERE [item_specification_ID] = @item_specification_ID";

                                SqlCommand cmd_deleteItemSpecification = new SqlCommand(str_deleteItemSpecification, conn);
                                cmd_deleteItemSpecification.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = itemSpecificationID;

                                cmd_deleteItemSpecification.ExecuteNonQuery();

                                if (itemIncomeFactorIDs != null)
                                {
                                    for (int i = 0; i < itemIncomeFactorIDs.Length; i++)
                                    {
                                        string str_getIncomeFactorIDNum = "SELECT COUNT([income_factor_ID]) " +
                                                                          "FROM [tblIncomeOperation] " +
                                                                          "WHERE [income_factor_ID] = @income_factor_ID";

                                        SqlCommand cmd_getIncomeFactorIDNum = new SqlCommand(str_getIncomeFactorIDNum, conn);
                                        cmd_getIncomeFactorIDNum.Parameters.Add("@income_factor_ID", SqlDbType.Int).Value = Convert.ToInt32(itemIncomeFactorIDs[i]);

                                        SqlDataAdapter adp_getIncomeFactorIDNum = new SqlDataAdapter(cmd_getIncomeFactorIDNum);
                                        DataTable dt_getIncomeFactorIDNum = new DataTable();
                                        adp_getIncomeFactorIDNum.Fill(dt_getIncomeFactorIDNum);

                                        if ((int)dt_getIncomeFactorIDNum.Rows[0][0] == 0)
                                        {
                                            string str_deleteIncomeFactor = "DELETE FROM [tblIncomeFactor] " +
                                                                            "WHERE [income_factor_ID] = @income_factor_ID";

                                            SqlCommand cmd_deleteIncomeFactor = new SqlCommand(str_deleteIncomeFactor, conn);
                                            cmd_deleteIncomeFactor.Parameters.Add("@income_factor_ID", SqlDbType.Int).Value = Convert.ToInt32(itemIncomeFactorIDs[i]);

                                            cmd_deleteIncomeFactor.ExecuteNonQuery();
                                        }
                                    }
                                }

                                if (itemSellFactorIDs != null)
                                {
                                    for (int i = 0; i < itemSellFactorIDs.Length; i++)
                                    {
                                        string str_getSellFactorIDNum = "SELECT COUNT([sell_factor_ID]) " +
                                                                        "FROM [tblSellOperation] " +
                                                                        "WHERE [sell_factor_ID] = @sell_factor_ID";

                                        SqlCommand cmd_getSellFactorIDNum = new SqlCommand(str_getSellFactorIDNum, conn);
                                        cmd_getSellFactorIDNum.Parameters.Add("@sell_factor_ID", SqlDbType.Int).Value = Convert.ToInt32(itemSellFactorIDs[i]);

                                        SqlDataAdapter adp_getSellFactorIDNum = new SqlDataAdapter(cmd_getSellFactorIDNum);
                                        DataTable dt_getSellFactorIDNum = new DataTable();
                                        adp_getSellFactorIDNum.Fill(dt_getSellFactorIDNum);

                                        if ((int)dt_getSellFactorIDNum.Rows[0][0] == 0)
                                        {
                                            string str_deleteSellFactor = "DELETE FROM [tblSellFactor] " +
                                                                          "WHERE [sell_factor_ID] = @sell_factor_ID";

                                            SqlCommand cmd_deleteSellFactor = new SqlCommand(str_deleteSellFactor, conn);
                                            cmd_deleteSellFactor.Parameters.Add("@sell_factor_ID", SqlDbType.Int).Value = Convert.ToInt32(itemSellFactorIDs[i]);

                                            cmd_deleteSellFactor.ExecuteNonQuery();
                                        }
                                    }
                                }

                                string str_getItemNum = "SELECT COUNT([item_specification_ID]) " +
                                                        "FROM [tblItemSpecification] " +
                                                        "WHERE [item_ID] = @item_ID";

                                SqlCommand cmd_getItemNum = new SqlCommand(str_getItemNum, conn);
                                cmd_getItemNum.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                                SqlDataAdapter adp_getItemNum = new SqlDataAdapter(cmd_getItemNum);
                                DataTable dt_getItemNum = new DataTable();
                                adp_getItemNum.Fill(dt_getItemNum);

                                if ((int)dt_getItemNum.Rows[0][0] == 0)
                                {
                                    string str_deleteItem = "DELETE FROM [tblItem] " +
                                                            "WHERE [item_ID] = @item_ID";

                                    SqlCommand cmd_deleteItem = new SqlCommand(str_deleteItem, conn);
                                    cmd_deleteItem.Parameters.Add("@item_ID", SqlDbType.Int).Value = itemID;

                                    cmd_deleteItem.ExecuteNonQuery();
                                }

                                conn.Close();

                                MessageBox.Show("کالای مورد نظر با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                                InitializeStoreManagementManageItem();

                                cmbStoreManageItemsNames.Focus();
                            }
                            else
                                MessageBox.Show("کالا یافت نشد", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        }
                    }
                }
                else
                {
                    txtStoreManageItemsNames.Visible = txtStoreManageItemSizes.Visible = txtStoreManageItemColors.Visible = cmbStoreManageItemDefIsMaleValues.Visible = false;
                    cmbStoreManageItemsNames.Visible = cmbStoreManageItemSizes.Visible = cmbStoreManageItemColors.Visible = cmbStoreManageItemIsMaleValues.Visible = true;
                    lblStoreManageItemPrice.Visible = txtStoreManageItemPrice.Visible = false;

                    btnStoreManageDeleteItem.Text = "حذف";
                    btnStoreManageEditItem.Text = "ویرایش";
                }
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Exam    

        private void txtExamSearchAthlete_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtExamSerial_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtExamEnglishName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("en");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtExamDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbExamRank_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtExamPrice_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtExamSerial_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private bool IsEnglishCharacter(char ch)
        {
            try
            {
                if (ch >= 97 && ch <= 122 || ch >= 65 && ch <= 90)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return false;
        }

        private void txtExamEnglishName_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                Keys key = (Keys)e.KeyChar;

                if (key == Keys.Back || key == Keys.Space || key == Keys.LButton || key == Keys.Cancel)
                    return;

                if (!IsEnglishCharacter(e.KeyChar))
                    e.Handled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExamLastExamReturn_Click(object sender, EventArgs e)
        {
            try
            {
                dgvExamAthletes_SelectionChanged(sender, e);

                btnExamLastExamReturn.Visible = false;

                cmbExamRank.Enabled = true;
                mtxtExamDate.ReadOnly = false;
                rdbAccepted.Enabled = true;
                rdbFailed.Enabled = true;
                txtExamEnglishName.ReadOnly = false;
                txtExamPrice.ReadOnly = false;

                btnExamPrintLastRank.Text = "آخرین آزمون";
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExamSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string showActiveOnly = "1", showReadyAthletes;

                if (chbExamShowReadyAthletes.Checked == true)
                    showReadyAthletes = " WHERE [آماده آزمون] = N'بله'";
                else
                    showReadyAthletes = null;

                string str_presence = "SELECT * FROM [dbo].[GetExamData](" + showActiveOnly + ") " + showReadyAthletes;

                DataTable dt = DgvSearch(str_presence, "[نام] + ' ' + CONVERT(VARCHAR, [کد])", txtExamSearchAthlete.Text);

                dgvExamAthletes.DataSource = dt;

                if (dgvExamAthletes.Rows.Count == 0)
                    ResetExamPage();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtExamPrice_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvExamAthletes_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dgvExamAthletes.Rows)
                    if ((string)(row.Cells["آماده آزمون"].Value) == "بله")
                        row.DefaultCellStyle.BackColor = Color.LightBlue;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExamPrintLastRank_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvExamAthletes.Rows.Count != 0 && dgvExamAthletes.SelectedRows.Count != 0)
                {
                    if (btnExamPrintLastRank.Text == "آخرین آزمون")
                    {
                        string lastRank = "SELECT TOP(1) exam_ID, exam_serial, exam_date, isaccepted, english_name FROM tblAthleteExam WHERE athlete_ID = " + dgvExamAthletes.SelectedCells[0].Value + " AND isaccepted = 1 ORDER BY exam_date DESC";

                        SqlDataAdapter da_lastRank = new SqlDataAdapter(lastRank, conn);
                        DataTable dt_lastRank = new DataTable();
                        da_lastRank.Fill(dt_lastRank);

                        if (dt_lastRank.Rows.Count != 0)
                        {
                            cmbExamRank.SelectedValue = dt_lastRank.Rows[0].ItemArray[0].ToString();
                            txtExamSerial.Text = dt_lastRank.Rows[0].ItemArray[1].ToString();
                            mtxtExamDate.Text = ConvertDateTime.m2sh(Convert.ToDateTime(dt_lastRank.Rows[0].ItemArray[2])).Substring(0, 10);
                            rdbAccepted.Checked = Convert.ToInt32(dt_lastRank.Rows[0].ItemArray[3]) == 1 ? true : false;
                            txtExamEnglishName.Text = dt_lastRank.Rows[0].ItemArray[4].ToString();

                            btnExamPrintLastRank.Text = "چاپ";
                            btnExamLastExamReturn.Visible = true;

                            cmbExamRank.Enabled = false;
                            mtxtExamDate.ReadOnly = true;
                            rdbAccepted.Enabled = false;
                            rdbFailed.Enabled = false;
                            txtExamEnglishName.ReadOnly = true;
                            txtExamPrice.ReadOnly = true;

                            cmbExamRank_SelectedIndexChanged(sender, e);
                        }
                        else
                            MessageBox.Show("آزمون قبول شده ای وجود ندارد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                    else if (btnExamPrintLastRank.Text == "چاپ")
                    {
                        string str_course = "SELECT rank_ID FROM tblExam WHERE exam_ID = " + cmbExamRank.SelectedValue;

                        SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                        DataTable dt_course = new DataTable();
                        da_course.Fill(dt_course);

                        if (dt_course.Rows.Count != 0)
                        {

                            if (Convert.ToInt32(dt_course.Rows[0].ItemArray[0]) >= 8 && Convert.ToInt32(dt_course.Rows[0].ItemArray[0]) <= 14)
                                PrintSpecialRanks(ConvertDateTime.sh2m(mtxtExamDate.Text.Substring(0, 2) + " / " + mtxtExamDate.Text.Substring(5, 2) + " / " + mtxtExamDate.Text.Substring(10, 4)), txtExamSerial.Text, txtExamEnglishName.Text, lblExamAthleteFullName.Text, dgvExamAthletes["جنسیت", dgvExamAthletes.CurrentCell.RowIndex].Value.ToString());
                            else
                                PrintNormalRanks(ConvertDateTime.sh2m(mtxtExamDate.Text.Substring(0, 2) + " / " + mtxtExamDate.Text.Substring(5, 2) + " / " + mtxtExamDate.Text.Substring(10, 4)), txtExamSerial.Text, txtExamEnglishName.Text, lblExamAthleteFullName.Text, dgvExamAthletes["جنسیت", dgvExamAthletes.CurrentCell.RowIndex].Value.ToString());

                            cmbExamRank_SelectedIndexChanged(sender, e);
                        }

                        btnExamPrintLastRank.Text = "آخرین آزمون";
                        btnExamLastExamReturn.Visible = false;

                        cmbExamRank.Enabled = true;
                        mtxtExamDate.ReadOnly = false;
                        rdbAccepted.Enabled = true;
                        rdbFailed.Enabled = true;
                        txtExamEnglishName.ReadOnly = false;
                        txtExamPrice.ReadOnly = false;
                    }
                }
                else
                    MessageBox.Show("هنرجویی انتخاب نشده است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtExamDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtExamDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtExamDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void FillDgvExamAthletes(DataGridView dgvName)
        {
            try
            {
                string showActiveOnly = "1", showReadyAthletes;

                if (chbExamShowReadyAthletes.Checked == true)
                    showReadyAthletes = " WHERE [آماده آزمون] = N'بله'";
                else
                    showReadyAthletes = null;

                string str_presence = "SELECT * FROM [dbo].[GetExamData](" + showActiveOnly + ") " + showReadyAthletes;

                SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                DataSet ds_presence = new DataSet();
                da_presence.Fill(ds_presence);

                dgvName.DataSource = ds_presence.Tables[0];

                foreach (DataGridViewRow row in dgvExamAthletes.Rows)
                    if ((string)(row.Cells["آماده آزمون"].Value) == "بله")
                        row.DefaultCellStyle.BackColor = Color.LightBlue;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Payment

        private void mtxtPayDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtPayDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtPayDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetPayPage()
        {
            try
            {
                txtPayAmount.Text = "0";
                cmbPayAthlete.SelectedValue = -1;
                cmbPayAthlete.Text = "انتخاب هنرجو";

                cmbPayType.SelectedIndex = 0;

                lblPaySideID.Text = "کد";
                lblPaySideTelephone.Text = "شماره موبایل";
                lblPaySideCredit.Text = "اعتبار";
                lblPaySideName.Text = "نام و نام خانوادگی";
                lblPaySideRank.Text = "سطح";
                txtPayType.Text = "شارژ";
                lblPayTotalPrice.Text = "-";

                pcbPaySideAthletePic.Image = Properties.Resources.background_text;

                lblPaySideRank.BackColor = Color.LightGray;
                lblPaySideRank.ForeColor = SystemColors.ControlText;
                lblPaySideCredit.ForeColor = SystemColors.ControlText;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtPayAmount_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) || e.KeyCode == Keys.Subtract ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbPayType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string str_course;

                if (cmbPayAthlete.SelectedValue != null)
                {
                    if (cmbPayType.SelectedIndex == 0)
                        str_course = "SELECT dbo.GetAthleteCredit(" + cmbPayAthlete.SelectedValue + ")";
                    else
                        str_course = "SELECT dbo.GetAthleteStoreCredit(" + cmbPayAthlete.SelectedValue + ")";

                    SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                    DataTable dt_course = new DataTable();
                    da_course.Fill(dt_course);

                    if (dt_course.Rows.Count != 0)
                        lblPaySideCredit.Text = dt_course.Rows[0].ItemArray[0].ToString();

                    getCreditColor(lblPaySideCredit);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Reports

        private void txtReportsPresenceSearch_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsPresenceStartDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsPresenceEndDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtReportsAthletesSearch_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbReportsAthletesSalon_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbReportsAthletesSans_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtReportsIncomeAthlete_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsIncomeStartDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsIncomeEndDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtReportsStorageSearch_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsStorageEndDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsStorageStartDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsIncomeEndDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsIncomeEndDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsIncomeEndDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsStorageEndDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsStorageEndDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsStorageEndDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsStorageStartDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsStorageStartDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsStorageStartDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }


        private void mtxtReportsPresenceStartDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsPresenceStartDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsPresenceStartDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsPresenceEndDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsPresenceEndDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsPresenceEndDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtReportsIncomeStartDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtReportsIncomeStartDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtReportsIncomeStartDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsPresencePay_Click(object sender, EventArgs e)
        {
            try
            {
                if ((dgvReportsPresence.Rows.Count != 0) && (dgvReportsPresence.CurrentCell != null))
                {
                    btnPays_Click(sender, e);

                    txtPayType.Text = "پرداخت";

                    cmbPayAthlete.SelectedValue = dgvReportsPresence[0, dgvReportsPresence.CurrentCell.RowIndex].Value.ToString();

                    if (Convert.ToInt32(lblReportsPresenceCredit.Text) < 0)
                    {
                        txtPayAmount.Text = Math.Abs(Convert.ToInt32(lblReportsPresenceCredit.Text)).ToString();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbReportsAthletesSalon_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbReportsAthletesSalon.SelectedIndex == 2)
                {
                    cmbReportsAthletesSans.SelectedValue = -1;
                    cmbReportsAthletesSans.Text = "همه سانس ها";
                    cmbReportsAthletesSans.DataSource = null;
                    cmbReportsAthletesSans.Items.Clear();
                    cmbReportsAthletesSans.Enabled = false;
                }
                else
                {
                    getSansData(cmbReportsAthletesSalon.SelectedIndex == 1 ? true : false, cmbReportsAthletesSans);
                    cmbReportsAthletesSans.Enabled = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void tabControlReports_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportPresence"])
                {
                    mtxtReportsPresenceStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsPresenceEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                    FillDgvReportsPresence(dgvReportsPresence, "DESC");

                    txtReportsPresenceSearch.Text = "";

                    if (dgvReportsPresence.RowCount == 0)
                    {
                        pcbReportsPresence.Image = Properties.Resources.background_text;
                        lblReportsPresenceAthleteID.Text = "کد";
                        lblReportsPresenceName.Text = "نام و نام خانوادگی";
                        lblReportsPresenceTell.Text = "شماره موبایل";

                        lblReportsPresenceRank.Text = "سطح";
                        lblReportsPresenceRank.BackColor = Color.LightGray;
                        lblReportsPresenceRank.ForeColor = SystemColors.ControlText;

                        lblReportsPresenceCredit.Text = "اعتبار";
                        lblReportsPresenceCredit.ForeColor = SystemColors.ControlText;
                    }
                }
                else if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportAthlete"])
                {
                    FillDgvReportsAthleteDetails(dgvReportsAthletes, "DESC", 9);

                    txtReportsAthletesSearch.Text = "";

                    if (dgvReportsAthletes.RowCount == 0)
                    {
                        pcbReportsAthletePic.Image = Properties.Resources.background_text;
                        lblReportsAthleteID.Text = "کد";
                        lblReportsAthleteName.Text = "نام و نام خانوادگی";
                        lblReportsAthleteCellphone.Text = "شماره موبایل";

                        lblReportsAthleteRank.Text = "سطح";
                        lblReportsAthleteRank.BackColor = Color.LightGray;
                        lblReportsAthleteRank.ForeColor = SystemColors.ControlText;

                        lblReportsAthleteCredit.Text = "اعتبار";
                        lblReportsAthleteCredit.ForeColor = SystemColors.ControlText;
                    }

                    for (int i = 0; i < dgvReportsAthletes.RowCount; i++)
                    {
                        if ((int)dgvReportsAthletes["اعتبار", i].Value < 0)
                            dgvReportsAthletes["اعتبار", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };

                        if ((int)dgvReportsAthletes["اعتبار.ف", i].Value < 0)
                            dgvReportsAthletes["اعتبار.ف", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                    }
                }
                else if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportIncome"])
                {
                    mtxtReportsIncomeStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsIncomeEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                    FillDgvReportsIncome(dgvReportsIncome, "DESC");

                    SetDgvColumnColor(dgvReportsIncome, "اعتبار");

                    txtReportsIncomeAthlete.Text = "";

                    lblReportsIncomeCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "شارژ").ToString();
                    lblReportsIncomeTotalExam.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "آزمون").ToString();
                    lblReportsIncomeTotalShop.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "فروشگاه").ToString();
                    lblReportsIncomeTotalCourseCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "ثبت نام").ToString();

                    lblReportsIncomeTotalIncome.Text = (Convert.ToInt32(lblReportsIncomeTotalCourseCharge.Text) + Convert.ToInt32(lblReportsIncomeTotalExam.Text) + Convert.ToInt32(lblReportsIncomeTotalShop.Text) + Convert.ToInt32(lblReportsIncomeCharge.Text)).ToString();
                }
                else if (tabControlReports.SelectedTab == tabControlReports.TabPages["tabPageReportStorage"])
                {
                    mtxtReportsStorageStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsStorageEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    txtReportsStorageSearch.Text = "";

                    FillDgvReportsStorage(dgvReportsStorage, "DESC");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsPresenceFilter_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckDate(mtxtReportsPresenceStartDate) && CheckDate(mtxtReportsPresenceEndDate))
                {
                    txtReportsPresenceSearch.Clear();

                    FillDgvReportsPresence(dgvReportsPresence, "DESC");

                    if (dgvReportsPresence.Rows.Count == 0)
                    {
                        pcbReportsPresence.Image = Properties.Resources.background_text;

                        lblReportsPresenceAthleteID.Text = "کد";
                        lblReportsPresenceName.Text = "نام و نام خانوادگی";
                        lblReportsPresenceTell.Text = "شماره موبایل";
                        lblReportsPresenceRank.Text = "سطح";
                        lblReportsPresenceCredit.Text = "اعتبار";

                        lblReportsPresenceRank.BackColor = Color.LightGray;
                        lblReportsPresenceRank.ForeColor = SystemColors.ControlText;

                    }
                }
                else
                {
                    MessageBox.Show("تاریخ وارد شده نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    mtxtReportsPresenceStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsPresenceEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsStorage_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvReportsStorage.CurrentCell != null)
                    lblReportsStorageRemaining.Text = GetItemRemaining(Convert.ToInt32(dgvReportsStorage[0, dgvReportsStorage.CurrentCell.RowIndex].Value)).ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsStorageFilter_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckDate(mtxtReportsStorageStartDate) && CheckDate(mtxtReportsStorageEndDate))
                    FillDgvReportsStorage(dgvReportsStorage, "DESC");
                else
                {
                    MessageBox.Show("تاریخ وارد شده نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtReportsStorageStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsStorageEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }

                txtReportsStorageSearch.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsPresence_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 3)
                    if (firstTime)
                    {
                        FillDgvReportsPresence(dgvReportsPresence, "DESC");
                        firstTime = false;
                    }
                    else
                    {
                        FillDgvReportsPresence(dgvReportsPresence, "ASC");
                        firstTime = true;
                    }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsAthletes_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 2 || e.ColumnIndex == 9)
                    if (firstTime)
                    {
                        FillDgvReportsAthleteDetails(dgvReportsAthletes, "DESC", e.ColumnIndex);
                        firstTime = false;
                    }
                    else
                    {
                        FillDgvReportsAthleteDetails(dgvReportsAthletes, "ASC", e.ColumnIndex);
                        firstTime = true;
                    }

                for (int i = 0; i < dgvReportsAthletes.RowCount; i++)
                {
                    if ((int)dgvReportsAthletes["اعتبار", i].Value < 0)
                        dgvReportsAthletes["اعتبار", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
                for (int j = 0; j < dgvReportsAthletes.RowCount; j++)
                {
                    if ((int)dgvReportsAthletes["اعتبار.ف", j].Value < 0)
                        dgvReportsAthletes["اعتبار.ف", j].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsIncome_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 6)
                    if (firstTime)
                    {
                        FillDgvReportsIncome(dgvReportsIncome, "DESC");
                        firstTime = false;
                    }
                    else
                    {
                        FillDgvReportsIncome(dgvReportsIncome, "ASC");
                        firstTime = true;
                    }

                for (int i = 0; i < dgvReportsIncome.RowCount; i++)
                {
                    if ((int)dgvReportsIncome["اعتبار", i].Value < 0)
                        dgvReportsIncome["اعتبار", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsStorage_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 7)
                    if (firstTime)
                    {
                        FillDgvReportsStorage(dgvReportsStorage, "DESC");
                        firstTime = false;
                    }
                    else
                    {
                        FillDgvReportsStorage(dgvReportsStorage, "ASC");
                        firstTime = true;
                    }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsIncomeFilter_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckDate(mtxtReportsIncomeStartDate) && CheckDate(mtxtReportsIncomeEndDate))
                {
                    FillDgvReportsIncome(dgvReportsIncome, "DESC");

                    lblReportsIncomeTotalCourseCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "ثبت نام").ToString();
                    lblReportsIncomeTotalExam.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "آزمون").ToString();
                    lblReportsIncomeTotalShop.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "فروشگاه").ToString();
                    lblReportsIncomeCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "شارژ").ToString();
                    lblReportsIncomeTotalIncome.Text = (Convert.ToInt32(lblReportsIncomeTotalCourseCharge.Text) + Convert.ToInt32(lblReportsIncomeTotalExam.Text) + Convert.ToInt32(lblReportsIncomeTotalShop.Text) + Convert.ToInt32(lblReportsIncomeCharge.Text)).ToString();

                    txtReportsIncomeAthlete.Clear();

                }
                else
                {
                    MessageBox.Show("تاریخ وارد شده نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtReportsIncomeStartDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    mtxtReportsIncomeEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsAthletesFilter_Click(object sender, EventArgs e)
        {
            try
            {
                txtReportsAthletesSearch.Clear();

                FillDgvReportsAthleteDetails(dgvReportsAthletes, "DESC", 9);

                for (int i = 0; i < dgvReportsAthletes.RowCount; i++)
                {
                    if ((int)dgvReportsAthletes["اعتبار", i].Value < 0)
                        dgvReportsAthletes["اعتبار", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
                for (int j = 0; j < dgvReportsAthletes.RowCount; j++)
                {
                    if ((int)dgvReportsAthletes["اعتبار.ف", j].Value < 0)
                        dgvReportsAthletes["اعتبار.ف", j].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }

                if (dgvReportsAthletes.Rows.Count == 0)
                {
                    pcbReportsAthletePic.Image = Properties.Resources.background_text;
                    lblReportsAthleteCellphone.Text = "شماره موبایل";
                    lblReportsAthleteID.Text = "کد";
                    lblReportsAthleteName.Text = "نام و نام خانوادگی";

                    lblReportsAthleteRank.Text = "سطح";
                    lblReportsAthleteRank.BackColor = Color.LightGray;
                    lblReportsAthleteRank.ForeColor = SystemColors.ControlText;

                    lblReportsAthleteCredit.Text = "اعتبار";
                    lblReportsAthleteCredit.ForeColor = SystemColors.ControlText;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsPresenceExam_Click(object sender, EventArgs e)
        {
            try
            {
                if ((dgvReportsPresence.Rows.Count != 0) && (dgvReportsPresence.CurrentCell != null))
                {
                    btnExam_Click(sender, e);

                    SearchDgvSelect(dgvReportsPresence[0, dgvReportsPresence.CurrentCell.RowIndex].Value, dgvExamAthletes);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private int getDgvColoumnSum(DataGridView dgv, string payColoumn, string typeColoumn, string totalType)
        {
            try
            {
                int total = 0;

                for (int i = 0; i < dgv.RowCount; i++)
                    if (totalType == dgv[typeColoumn, i].Value.ToString())
                        total += (int)dgv[payColoumn, i].Value;

                return total;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private static void SetDgvColumnColor(DataGridView dgvName, string columnName)
        {
            try
            {
                for (int i = 0; i < dgvName.RowCount; i++)
                {
                    if ((int)dgvName[columnName, i].Value < 0)
                        dgvName[columnName, i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void FillDgvReportsPresence(DataGridView dgvName, string sortType)
        {
            try
            {
                if (mtxtReportsPresenceStartDate.Text == "   /    / " && mtxtReportsPresenceEndDate.Text == "   /    / ")
                    mtxtReportsPresenceStartDate.Text = mtxtReportsPresenceEndDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                if (mtxtReportsPresenceStartDate.Text != "   /    / " || mtxtReportsPresenceEndDate.Text != "   /    / ")
                {
                    DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsPresenceStartDate.Text);
                    DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsPresenceEndDate.Text);

                    string maleOnly = "آقا", femaleOnly = "خانم";

                    if (chbReportsPresenceMale.Checked == true)
                        femaleOnly = "آقا";
                    else
                        femaleOnly = "خانم";

                    if (chbReportsPresenceFemale.Checked == true)
                        maleOnly = "خانم";
                    else
                        maleOnly = "آقا";

                    string str_presence = "SELECT * " +
                                          "FROM view_athletePresence " +
                                          "WHERE (CONVERT(DATE, [ساعت ورود]) >= '" + startingTime.Date.ToString() + "' AND CONVERT(DATE, [ساعت ورود]) <= '" + endingTime.Date.ToString() + "' ) " +
                                          "AND ( جنسیت = N'" + maleOnly + "' OR جنسیت = N'" + femaleOnly + "' OR جنسیت = N'نامشخص' ) " +
                                          "ORDER BY [ساعت ورود] " + sortType;

                    SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                    DataSet ds_presence = new DataSet();
                    da_presence.Fill(ds_presence);

                    if (ds_presence.Tables[0].Rows.Count > 0)
                        foreach (DataRow row in ds_presence.Tables[0].Rows)
                            row["ساعت ورود"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["ساعت ورود"]));

                    dgvName.DataSource = ds_presence.Tables[0];

                    dgvName.Columns["ساعت ورود"].SortMode = DataGridViewColumnSortMode.NotSortable;

                    dgvName.Columns["کد"].Visible = false;

                    lblReportsPresenceTotal.Text = dgvName.Rows.Count.ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void FillDgvReportsAthleteDetails(DataGridView dgvName, string sortType, int index)
        {
            try
            {
                int showActiveOnly, maleOnly, femaleOnly;
                string debtorOnly, sans, sort = "ORDER BY [تاریخ ثبت نام] " + sortType;
                
                if (index == 2)
                    sort = "ORDER BY [تاریخ تولد] " + sortType;

                if (cmbReportsAthletesSans.SelectedValue != null)
                    sans = " (SELECT[athlete_ID] FROM [tblAthleteSans] WHERE [sans_ID] = CONVERT(VARCHAR," + cmbReportsAthletesSans.SelectedValue.ToString() + ")) [AS] ON [tblAthlete].[athlete_ID] = [AS].[athlete_ID] INNER JOIN ";
                else
                    sans = null;

                if (chbReportsAthletesOnlyDebtor.Checked == true)
                    debtorOnly = " < ";
                else
                    debtorOnly = " >= ";

                if (chbReportsAthletesDisactiveAthletes.Checked == true)
                    showActiveOnly = 0;
                else
                    showActiveOnly = 1;

                if (chbReportsAthletesMale.Checked == true)
                    femaleOnly = 1;
                else
                    femaleOnly = 0;

                if (chbReportsAthleteFemale.Checked == true)
                    maleOnly = 0;
                else
                    maleOnly = 1;

                string str_presence = "SELECT tblAthlete.athlete_ID AS 'کد', tblName.name_title + ' ' + tblAthlete.l_name AS 'نام', " +
                             "CASE WHEN tblAthlete.birth_date IS NULL THEN N'ث.ن' ELSE CONVERT(VARCHAR,tblAthlete.birth_date) END AS 'تاریخ تولد',CASE WHEN tblAthlete.n_code IS NULL THEN N'ث.ن' WHEN tblAthlete.n_code = '' THEN N'ث.ن' ELSE tblAthlete.n_code END AS 'کد ملی' , " +
                             "CASE WHEN tblAthlete.ismale = 1 THEN N'آقا' WHEN tblAthlete.ismale = 0 THEN N'خانم' ELSE N'ث.ن' END AS 'جنسیت' ," +
                             "CASE WHEN tblAthlete.telephone IS NULL THEN N'ث.ن' ELSE tblAthlete.telephone END AS 'شماره تماس', " +
                             "CASE WHEN dbo.GetAthleteSans(tblAthlete.athlete_ID) IS NULL THEN N'ث.ن' ELSE dbo.GetAthleteSans(tblAthlete.athlete_ID) END AS 'سانس', " +
                             "dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) AS 'اعتبار.ف',dbo.GetAthleteCredit(tblAthlete.athlete_ID) AS 'اعتبار', " +
                             "CONVERT(VARCHAR, tblAthlete.register_date) AS 'تاریخ ثبت نام', " +
                             "CASE WHEN tblAthlete.isActive = 1 THEN N'فعال' WHEN tblAthlete.isActive = 0 THEN N'غیر فعال' ELSE N'ث.ن' END AS 'وضعیت'" +
                             "FROM tblAthlete INNER JOIN " +
                             sans +
                             "tblName ON tblAthlete.name_ID = tblName.name_ID " +
                             "WHERE ( tblAthlete.isactive = 1 OR tblAthlete.isactive = " + showActiveOnly + " ) " +
                             "AND(tblAthlete.ismale = " + maleOnly + " OR tblAthlete.ismale = " + femaleOnly + "  OR tblAthlete.ismale IS NULL ) " +
                             "AND(dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) < 0 OR dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) " + debtorOnly + " 0 " +
                             "OR dbo.GetAthleteCredit(tblAthlete.athlete_ID) < 0 OR dbo.GetAthleteCredit(tblAthlete.athlete_ID) " + debtorOnly + " 0 ) " +
                             sort;


                SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                DataSet ds_presence = new DataSet();
                da_presence.Fill(ds_presence);

                if (ds_presence.Tables[0].Rows.Count != 0)
                {
                    foreach (DataRow row in ds_presence.Tables[0].Rows)
                    {
                        if (row["تاریخ تولد"].ToString() != "" && row["تاریخ تولد"].ToString() != "ث.ن")
                        {
                            string birthDate = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ تولد"]));
                            birthDate = birthDate.Substring(6, 4) + '/' + birthDate.Substring(3, 2) + '/' + birthDate.Substring(0, 2);
                            row["تاریخ تولد"] = birthDate;
                        }

                        row["تاریخ ثبت نام"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ ثبت نام"]));
                    }
                }

                dgvName.DataSource = ds_presence.Tables[0];

                dgvName.Columns["تاریخ ثبت نام"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvName.Columns["تاریخ تولد"].SortMode = DataGridViewColumnSortMode.NotSortable;

                lblReportsAthletesTotalPersons.Text = dgvName.Rows.Count.ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void FillDgvReportsIncome(DataGridView dgvName, string sortType)
        {
            try
            {
                DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsIncomeStartDate.Text);
                DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsIncomeEndDate.Text);

                string shop = "2", register = "4", exam = "3", charge = "1";
                int maleOnly = 1, femaleOnly = 1;

                if (chbReportsIncomeExam.Checked == true)
                    exam = "3";
                else
                    exam = " NULL ";

                if (chbReportsIncomeRegister.Checked == true)
                    register = "4";
                else
                    register = " NULL ";

                if (chbReportsIncomeShop.Checked == true)
                    shop = "2";
                else
                    shop = " NULL ";

                if (chbReportsIncomeCharge.Checked == true)
                    charge = "1";
                else
                    charge = " NULL ";

                if (chbReportsIncomeFemale.Checked == true)
                    femaleOnly = 0;
                else
                    femaleOnly = 1;

                if (chbReportsIncomeMale.Checked == true)
                    maleOnly = 1;
                else
                    maleOnly = 0;

                if (shop == " NULL " && exam == " NULL " && register == " NULL " && charge == " NULL ")
                {
                    exam = "3";
                    register = "4";
                    shop = "2";
                    charge = "1";
                }

                string str_presence = "SELECT tblAthlete.athlete_ID AS N'کد', tblName.name_title + ' ' + tblAthlete.l_name AS N'نام', " +
                             "CASE WHEN tblAthlete.ismale = 1 THEN N'آقا' WHEN tblAthlete.ismale = 0 THEN N'خانم' ELSE N'ث.ن' END AS N'جنسیت' , CASE WHEN tblAthlete.cellphone IS NULL THEN N'ث.ن' ELSE tblAthlete.cellphone END AS N'شماره تماس', " +
                             "tblPayType.payType_title AS N'بخش',tblAthleteCharge.athlete_pay AS N'مبلغ پرداختی' ,CONVERT(VARCHAR, tblAthleteCharge.charge_date) AS N'تاریخ',CASE WHEN tblPayType.payType_title = N'فروشگاه' THEN [dbo].[GetAthleteStoreCredit](tblAthlete.athlete_ID) ELSE dbo.GetAthleteCredit(tblAthlete.athlete_ID) END AS 'اعتبار' " +
                             "FROM tblAthlete INNER JOIN " +
                             "tblName ON tblAthlete.name_ID = tblName.name_ID INNER JOIN " +
                             "tblAthleteCharge ON tblAthlete.athlete_ID = tblAthleteCharge.athlete_ID INNER JOIN " +
                             "tblPayType ON tblAthleteCharge.payType_ID = tblPayType.payType_ID " +
                             "WHERE (tblAthlete.ismale = " + maleOnly + " OR tblAthlete.ismale = " + femaleOnly + "  OR tblAthlete.ismale IS NULL) " +
                             "AND (tblAthleteCharge.payType_ID = " + exam + " OR tblAthleteCharge.payType_ID = " + shop + " OR tblAthleteCharge.payType_ID = " + register + " OR tblAthleteCharge.payType_ID = " + charge + ") " +
                             "AND CONVERT(DATE, tblAthleteCharge.charge_date) >= '" + startingTime.Date + "' AND CONVERT(DATE, tblAthleteCharge.charge_date) <= '" + endingTime.Date + "'" +
                             "ORDER BY [تاریخ] " + sortType;

                SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                DataSet ds_presence = new DataSet();
                da_presence.Fill(ds_presence);

                if (ds_presence.Tables[0].Rows.Count != 0)
                {
                    foreach (DataRow row in ds_presence.Tables[0].Rows)
                    {
                        row["تاریخ"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ"]));

                        if ((int)row["مبلغ پرداختی"] <= 0)
                            row.Delete();
                    }
                }

                dgvName.DataSource = ds_presence.Tables[0];

                dgvName.Columns["تاریخ"].SortMode = DataGridViewColumnSortMode.NotSortable;

            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void FillDgvReportsStorage(DataGridView dgvName, string sortType)
        {
            try
            {
                DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsStorageStartDate.Text);
                DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsStorageEndDate.Text);

                int total = 0;

                string str_presence = "SELECT [tblItemSpecification].[item_specification_ID], [tblSellFactor].[sell_factor_ID] AS [فاکتور], " +
                                      "[tblSellFactor].[athlete_ID] AS[کد هنرجو], [tblItem].[item_name] AS[نام کالا], " +
                                      "CASE WHEN[tblColor].[color_name] IS NULL THEN N'ث.ن' ELSE[tblColor].[color_name] END AS[رنگ], " +
                                      "CASE WHEN[tblItemSpecification].[size] IS NULL THEN N'ث.ن' ELSE[tblItemSpecification].[size] END AS[سایز], " +
                                      "CASE WHEN[tblItemSpecification].[ismale] = 1 THEN N'آقایان' WHEN[tblItemSpecification].[ismale] = 0 THEN N'بانوان' ELSE N'ث.ن' END AS[جنسیت], " +
                                      "CONVERT(VARCHAR, [tblSellFactor].[factor_date]) AS[تاریخ فروش], [tblSellOperation].[number] AS[تعداد], " +
                                      "[dbo].[GetItemPrice] ([tblItemSpecification].[item_specification_ID], [tblSellFactor].[factor_date]) AS[قیمت] FROM[tblSellFactor] " +
                                      "INNER JOIN[tblSellOperation] ON[tblSellFactor].[sell_factor_ID] = [tblSellOperation].[sell_factor_ID] " +
                                      "INNER JOIN[tblItemSpecification] ON[tblSellOperation].[item_specification_ID] = [tblItemSpecification].[item_specification_ID] " +
                                      "INNER JOIN[tblItem] ON[tblItemSpecification].[item_ID] = [tblItem].[item_ID] " +
                                      "LEFT JOIN[tblColor] ON[tblItemSpecification].[color_ID] = [tblColor].[color_ID] " +
                                      "WHERE CONVERT(DATE, tblSellFactor.factor_date) >= '" + startingTime + "' AND CONVERT(DATE, tblSellFactor.factor_date) <= '" + endingTime + "'" +
                                      "ORDER BY [تاریخ فروش] " + sortType;


                SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                DataSet ds_presence = new DataSet();
                da_presence.Fill(ds_presence);

                if (ds_presence.Tables[0].Rows.Count != 0)
                    foreach (DataRow row in ds_presence.Tables[0].Rows)
                        row["تاریخ فروش"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ فروش"]));

                dgvName.DataSource = ds_presence.Tables[0];

                dgvName.Columns["تاریخ فروش"].SortMode = DataGridViewColumnSortMode.NotSortable;

                dgvName.Columns[0].Visible = false;

                for (int i = 0; i < dgvReportsStorage.RowCount; i++)
                    total += (int)dgvReportsStorage["قیمت", i].Value;

                lblReportsStorageTotalSell.Text = total.ToString();

                if (dgvName.RowCount > 0)
                {
                    dgvName.CurrentCell = dgvName[1, 0];
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private DataTable DgvSearch(string query, string columnNameSearchSource, string search)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                string query_upper = query.ToUpper();

                if (!query_upper.Contains("WHERE"))
                {
                    if (!query_upper.Contains("ORDER BY"))
                    {
                        sb.Append(query);
                        sb.Append(" WHERE ");
                        sb.Append(columnNameSearchSource);
                        sb.Append(" LIKE N'%");
                        sb.Append(search);
                        sb.Append("%'");
                    }
                    else
                    {
                        int index = query_upper.IndexOf("ORDER BY");

                        sb.Append("WHERE ");
                        sb.Append(columnNameSearchSource);
                        sb.Append(" LIKE N'%");
                        sb.Append(search);
                        sb.Append("%'");
                        sb.Append(" ");

                        query = query.Insert(index, sb.ToString());

                        sb.Clear();
                        sb.Append(query);
                    }
                }
                else
                {
                    int index = query_upper.IndexOf("WHERE");

                    sb.Append(" ");
                    sb.Append(columnNameSearchSource);
                    sb.Append(" LIKE N'%");
                    sb.Append(search);
                    sb.Append("%'");
                    sb.Append(" AND ");

                    query = query.Insert(index + 5, sb.ToString());

                    sb.Clear();
                    sb.Append(query);
                }

                SqlDataAdapter adp = new SqlDataAdapter(new SqlCommand(sb.ToString(), conn));
                adp.SelectCommand.CommandTimeout = 120;

                DataTable dt = new DataTable();
                adp.Fill(dt);

                return dt;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }



        #endregion

        #region Settings

        private void mtxtPayDate_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtPayType_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbPayAthlete_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbPayType_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtPayAmount_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditExam_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsExamPrice_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsCourseSections_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsCoursePrice_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditAgeCategory_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsAgeCategoryName_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsAgeCategoryAgeFrom_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsAgeCategoryAgeTo_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditSansGender_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditSans_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsSansAgeCategory_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsSansGender_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtSettingsSansStartTime_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtSettingsSansEndTime_Enter(object sender, EventArgs e)
        {
            try
            {
                SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsExamPrice_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsCourseSections_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsCoursePrice_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsAgeCategoryAgeFrom_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtSettingsAgeCategoryAgeTo_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                    e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }


        private void btnSettingsReturnAgeCategory_Click(object sender, EventArgs e)
        {
            try
            {
                btnSettingsAddAgeCategory.Text = "افزودن";

                cmbSettingsEditAgeCategory.SelectedValue = 0;
                txtSettingsAgeCategoryName.Text = null;
                txtSettingsAgeCategoryAgeFrom.Text = null;
                txtSettingsAgeCategoryAgeTo.Text = null;

                btnSettingsReturnAgeCategory.Visible = false;
                btnSettingsAgeCategoryRemove.Visible = false;

                cmbSettingsEditAgeCategory.Text = "انتخاب گروه سنی";
                cmbSettingsEditAgeCategory.Focus();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnSettingsReturnSans_Click(object sender, EventArgs e)
        {
            try
            {
                btnSettingsAddSans.Text = "افزودن";

                cmbSettingsSansGender.SelectedIndex = -1;
                cmbSettingsSansAgeCategory.SelectedValue = 0;
                cmbSettingsEditSansGender.SelectedIndex = -1;
                cmbSettingsEditSans.SelectedIndex = -1;
                mtxtSettingsSansStartTime.Text = null;
                mtxtSettingsSansEndTime.Text = null;

                btnSettingsReturnSans.Visible = false;
                btnSettingsSansRemove.Visible = false;

                cmbSettingsEditSans.Text = "ابتدا سالن را انتخاب کنید";
                cmbSettingsEditSansGender.Text = "انتخاب سالن";
                cmbSettingsEditSans.Enabled = false;

                cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";
                cmbSettingsSansGender.Text = "انتخاب سالن";

                cmbSettingsEditSansGender.Focus();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnSettingsEditUser_Click(object sender, EventArgs e)
        {
            try
            {
                conn.Open();

                SqlCommand com = new SqlCommand("UPDATE tblCharge SET sections = @sections , price  = @price , price_date = @priceDate WHERE charge_ID = 1 ", conn);

                com.Parameters.AddWithValue("@sections", txtSettingsCourseSections.Text.Trim());
                com.Parameters.AddWithValue("@price", txtSettingsCoursePrice.Text.Trim());
                com.Parameters.AddWithValue("@priceDate", System.DateTime.Now);

                com.ExecuteNonQuery();

                lblMoneyPerSection.Text = MoneyPerSection().ToString();

                conn.Close();

                MessageBox.Show("تغییرات با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditSansGender_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                getSansData(cmbSettingsEditSansGender.SelectedIndex == 1 ? true : false, cmbSettingsEditSans);
                cmbSettingsEditSans.Enabled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void getCourseCharge(Label lblCoursePrice, TextBox lblSections, Label lblSectionsPrice)
        {
            try
            {
                int sessions, eachSessionPrice, coursePrice;
                string str_course = "SELECT price , sections FROM tblCharge";

                SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                DataTable dt_course = new DataTable();
                da_course.Fill(dt_course);

                if (dt_course.Rows.Count != 0)
                {
                    int sessionsPerWeek;
                    int remainDays;
                    int remainWeeks;
                    int remainSessions;

                    coursePrice = Convert.ToInt32(dt_course.Rows[0].ItemArray[0]);
                    sessions = Convert.ToInt32(dt_course.Rows[0].ItemArray[1]);


                    int today = Convert.ToInt32(ConvertDateTime.m2sh(DateTime.Now).Substring(0, 2));
                    int month = Convert.ToInt32(ConvertDateTime.m2sh(DateTime.Now).Substring(3, 2));

                    int days = DateTime.DaysInMonth(DateTime.Now.Year, month);

                    eachSessionPrice = coursePrice / sessions;

                    sessionsPerWeek = sessions / 4;
                    remainDays = days - today;
                    remainWeeks = remainDays / 7;
                    remainSessions = remainWeeks * sessionsPerWeek;
                    remainSessions += (remainDays % 7) / 2;

                    lblCoursePrice.Text = (remainSessions * eachSessionPrice).ToString();
                    lblSections.Text = remainSessions.ToString();
                    lblSectionsPrice.Text = eachSessionPrice.ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void getRankData(ComboBox cmbRankName)
        {
            try
            {
                string str_rank = "SELECT rank_ID , rank_name FROM tblRank";

                SqlDataAdapter da_rank = new SqlDataAdapter(str_rank, conn);
                DataSet ds_rank = new DataSet();
                da_rank.Fill(ds_rank);

                cmbRankName.DisplayMember = "rank_name";
                cmbRankName.ValueMember = "rank_ID";
                cmbRankName.DataSource = ds_rank.Tables[0];
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void getCourseDataSettings()
        {
            try
            {
                string str_course = "SELECT sections, price, price_date FROM tblCharge WHERE charge_ID = 1";

                SqlDataAdapter da_ageCategory = new SqlDataAdapter(str_course, conn);
                DataTable dt_ageCategory = new DataTable();
                da_ageCategory.Fill(dt_ageCategory);

                if (dt_ageCategory.Rows.Count != 0)
                {
                    txtSettingsCourseSections.Text = dt_ageCategory.Rows[0].ItemArray[0].ToString();
                    txtSettingsCoursePrice.Text = dt_ageCategory.Rows[0].ItemArray[1].ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void getAgeCategoryData(ComboBox cmbAgeCategory)
        {
            try
            {
                string str_AgeCategory = "SELECT age_category_ID , age_category_name FROM tblAgeCategory";

                SqlDataAdapter da_AgeCategory = new SqlDataAdapter(str_AgeCategory, conn);
                DataSet ds_AgeCategory = new DataSet();
                da_AgeCategory.Fill(ds_AgeCategory);

                cmbAgeCategory.DisplayMember = "age_category_name";
                cmbAgeCategory.ValueMember = "age_category_ID";
                cmbAgeCategory.DataSource = ds_AgeCategory.Tables[0];
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void getSansData(bool ismale, ComboBox cmbSansName)
        {
            try
            {
                string str_sans = "SELECT sans_ID , CONVERT(varchar, [tblSans].[end_time] , 8  ) + ' - ' +  CONVERT(varchar, [tblSans].[start_time]  , 8) AS sansTime FROM tblSans WHERE ismale = " + (ismale == true ? 1 : 0) + " ORDER BY start_time ";
                SqlDataAdapter da_sans = new SqlDataAdapter(str_sans, conn);
                DataSet ds_sans = new DataSet();
                da_sans.Fill(ds_sans);

                cmbSansName.DisplayMember = "sansTime";
                cmbSansName.ValueMember = "sans_ID";
                cmbSansName.DataSource = ds_sans.Tables[0];
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public void getExamData(ComboBox cmbExamName)
        {
            try
            {
                string str_exam = "SELECT tblExam.exam_ID, tblRank.rank_name " +
                 "FROM tblExam INNER JOIN " +
                 "tblRank ON tblExam.rank_ID = tblRank.rank_ID";

                SqlDataAdapter da_exam = new SqlDataAdapter(str_exam, conn);
                DataSet ds_exam = new DataSet();
                da_exam.Fill(ds_exam);

                cmbExamName.DisplayMember = "rank_name";
                cmbExamName.ValueMember = "exam_ID";
                cmbExamName.DataSource = ds_exam.Tables[0];
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private int MoneyPerSection()
        {
            try
            {
                int sections = 1;
                int price = 1;
                int moneyPerSection = 0;

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                string str_course = "SELECT sections , price FROM tblCharge";

                SqlDataAdapter da_presence = new SqlDataAdapter(str_course, conn);
                DataTable dt_presence = new DataTable();
                da_presence.Fill(dt_presence);

                if (dt_presence.Rows.Count != 0 && Convert.ToInt32(dt_presence.Rows[0].ItemArray[0]) != 0)
                {
                    sections = Convert.ToInt32(dt_presence.Rows[0].ItemArray[0]);
                    price = Convert.ToInt32(dt_presence.Rows[0].ItemArray[1]);

                    moneyPerSection = price / sections;
                }

                conn.Close();

                return moneyPerSection;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private void btnSettingsUser_Click(object sender, EventArgs e)
        {
            try
            {
                if (!txtSettingsUserOldPassword.Enabled)
                {
                    string txtSettingsUserName_trimmed = txtSettingsUserName.Text.Trim();

                    if (txtSettingsUserName_trimmed == "")
                    {
                        MessageBox.Show("قسمت 'نام کاربری' خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        txtSettingsUserName.Focus();
                    }
                    else if (txtSettingsUserNewPassword.Text.Trim() == "")
                    {
                        MessageBox.Show("قسمت 'رمز عبور' خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        txtSettingsUserNewPassword.Focus();
                    }
                    else if (txtSettingsUserName_trimmed == "Admin")
                    {
                        MessageBox.Show("تغییر رمز این نام کاربری مجاز نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        txtSettingsUserName.ResetText();
                        txtSettingsUserNewPassword.ResetText();

                        txtSettingsUserName.Focus();
                    }
                    else
                    {
                        string str_getUsername = "SELECT [username] " +
                                                 "FROM [tblUser] " +
                                                 "WHERE [username] = @username";

                        SqlCommand cmd_getUsername = new SqlCommand(str_getUsername, conn);
                        cmd_getUsername.Parameters.Add("@username", SqlDbType.NVarChar).Value = txtSettingsUserName.Text;

                        SqlDataAdapter adp_getUsername = new SqlDataAdapter(cmd_getUsername);
                        DataTable dt_getUsername = new DataTable();
                        adp_getUsername.Fill(dt_getUsername);

                        if (dt_getUsername.Rows.Count > 0)
                        {
                            string str_updateUserPass = "UPDATE [tblUser] " +
                                                        "SET [user_pass] = @user_pass " +
                                                        "WHERE [username] = @username";

                            SqlCommand cmd_updateUserPass = new SqlCommand(str_updateUserPass, conn);
                            cmd_updateUserPass.Parameters.Add("@user_pass", SqlDbType.NVarChar).Value = fLogin.Cal_MD5(txtSettingsUserNewPassword.Text);
                            cmd_updateUserPass.Parameters.Add("@username", SqlDbType.NVarChar).Value = txtSettingsUserName.Text;

                            conn.Open();
                            cmd_updateUserPass.ExecuteNonQuery();
                            conn.Close();

                            MessageBox.Show("رمز عبور با موفقیت تغییر یافت", "عملیات موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            txtSettingsUserOldPassword.ResetText();
                            txtSettingsUserNewPassword.ResetText();

                            txtSettingsUserOldPassword.Focus();
                        }
                        else
                        {
                            MessageBox.Show("نام کاربری موجود نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            txtSettingsUserName.ResetText();
                            txtSettingsUserNewPassword.ResetText();

                            txtSettingsUserName.Focus();
                        }
                    }
                }
                else if (txtSettingsUserOldPassword.Text.Trim() == "")
                {
                    MessageBox.Show("قسمت 'رمز عبور فعلی' خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    txtSettingsUserOldPassword.Focus();
                }
                else if (txtSettingsUserNewPassword.Text.Trim() == "")
                {
                    MessageBox.Show("قسمت 'رمز عبور جدید' خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    txtSettingsUserNewPassword.Focus();
                }
                else
                {
                    string str_getUserPass = "SELECT [user_pass] " +
                                             "FROM [tblUser] " +
                                             "WHERE [username] = @username " +
                                             "    AND [user_pass] = @user_pass";

                    SqlCommand cmd_getUserPass = new SqlCommand(str_getUserPass, conn);
                    cmd_getUserPass.Parameters.Add("@username", SqlDbType.NVarChar).Value = txtSettingsUserName.Text;
                    cmd_getUserPass.Parameters.Add("@user_pass", SqlDbType.NVarChar).Value = fLogin.Cal_MD5(txtSettingsUserOldPassword.Text);

                    SqlDataAdapter adp_getUserPass = new SqlDataAdapter(cmd_getUserPass);
                    DataTable dt_getUserPass = new DataTable();
                    adp_getUserPass.Fill(dt_getUserPass);

                    if (dt_getUserPass.Rows.Count > 0)
                    {
                        string str_updateUserPass = "UPDATE [tblUser] " +
                                                    "SET [user_pass] = @user_pass " +
                                                    "WHERE [username] = @username";

                        SqlCommand cmd_updateUserPass = new SqlCommand(str_updateUserPass, conn);
                        cmd_updateUserPass.Parameters.Add("@user_pass", SqlDbType.NVarChar).Value = fLogin.Cal_MD5(txtSettingsUserNewPassword.Text);
                        cmd_updateUserPass.Parameters.Add("@username", SqlDbType.NVarChar).Value = txtSettingsUserName.Text;

                        conn.Open();
                        cmd_updateUserPass.ExecuteNonQuery();
                        conn.Close();

                        MessageBox.Show("رمز عبور با موفقیت تغییر یافت", "عملیات موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        txtSettingsUserOldPassword.ResetText();
                        txtSettingsUserNewPassword.ResetText();

                        txtSettingsUserOldPassword.Focus();
                    }
                    else
                    {
                        MessageBox.Show("رمز عبور اشتباه است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        txtSettingsUserOldPassword.ResetText();
                        txtSettingsUserNewPassword.ResetText();

                        txtSettingsUserOldPassword.Focus();
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtSettingsSansStartTime_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtSettingsSansStartTime.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtSettingsSansStartTime.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtSettingsSansEndTime_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtSettingsSansEndTime.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtSettingsSansEndTime.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnAddSans_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbSettingsSansAgeCategory.SelectedValue == null)
                {
                    MessageBox.Show("گروه سنی نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (cmbSettingsSansGender.SelectedIndex != 0 && cmbSettingsSansGender.SelectedIndex != 1)
                {
                    MessageBox.Show("سالن نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (mtxtSettingsSansStartTime.Text == "  :")
                {
                    MessageBox.Show("زمان شروع وارد نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (mtxtSettingsSansEndTime.Text == "  :")
                {
                    MessageBox.Show("زمان پایان وارد نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (mtxtSettingsSansStartTime.Text.Length < 5 ||
                    mtxtSettingsSansEndTime.Text.Length < 5)
                {
                    MessageBox.Show("زمان وارد شده نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (Convert.ToInt32(mtxtSettingsSansStartTime.Text.Substring(0, 2)) > 23 || Convert.ToInt32(mtxtSettingsSansStartTime.Text.Substring(3, 2)) > 59 ||
                        Convert.ToInt32(mtxtSettingsSansEndTime.Text.Substring(0, 2)) > 23 || Convert.ToInt32(mtxtSettingsSansEndTime.Text.Substring(3, 2)) > 59)
                {
                    MessageBox.Show("زمان وارد شده اشتباه است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else if (Convert.ToInt32(mtxtSettingsSansStartTime.Text.Replace(":", "")) >= Convert.ToInt32(mtxtSettingsSansEndTime.Text.Replace(":", "")))
                {
                    MessageBox.Show("خطا در بازه زمانی", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    return;
                }
                else
                {
                    conn.Open();

                    if (btnSettingsAddSans.Text == "ثبت ویرایش")
                    {
                        SqlCommand com = new SqlCommand("UPDATE tblSans SET ismale = @isMale , age_category_ID = @ageCategoryID , start_time = @startTime, end_time = @endTime WHERE sans_ID = " + cmbSettingsEditSans.SelectedValue, conn);

                        com.Parameters.AddWithValue("@isMale", cmbSettingsSansGender.SelectedIndex);
                        com.Parameters.AddWithValue("@ageCategoryID", cmbSettingsSansAgeCategory.SelectedValue);
                        com.Parameters.AddWithValue("@startTime", mtxtSettingsSansStartTime.Text.Trim());
                        com.Parameters.AddWithValue("@endTime", mtxtSettingsSansEndTime.Text.Trim());

                        com.ExecuteNonQuery();

                        MessageBox.Show("تغییرات با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        btnSettingsAddSans.Text = "افزودن";

                        btnSettingsSansRemove.Visible = false;
                        btnSettingsReturnSans.Visible = false;

                        mtxtSettingsSansStartTime.Clear();
                        mtxtSettingsSansEndTime.Clear();
                        cmbSettingsSansAgeCategory.SelectedIndex = -1;
                        cmbSettingsSansGender.SelectedIndex = -1;

                        cmbSettingsEditSans.Text = "ابتدا سالن را انتخاب کنید";
                        cmbSettingsEditSansGender.Text = "انتخاب سالن";
                        cmbSettingsEditSans.Enabled = false;

                        cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";
                        cmbSettingsSansGender.Text = "انتخاب سالن";

                        cmbSettingsEditSansGender.Focus();

                        InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                        InitializeMainSideDaySans(dgvMainSideSanses);
                    }
                    else
                    {
                        if (cmbSettingsSansGender.SelectedIndex != -1 && cmbSettingsSansAgeCategory.SelectedValue != null && mtxtSettingsSansStartTime.Text.Trim() != "" && mtxtSettingsSansEndTime.Text.Trim() != "")
                        {
                            SqlCommand add_sans = new SqlCommand("INSERT INTO tblSans (ismale , age_category_ID , start_time, end_time) Values (@isMale,@ageCategoryID,@startTime,@endTime)", conn);

                            add_sans.Parameters.AddWithValue("@isMale", cmbSettingsSansGender.SelectedIndex);
                            add_sans.Parameters.AddWithValue("@ageCategoryID", cmbSettingsSansAgeCategory.SelectedValue);
                            add_sans.Parameters.AddWithValue("@startTime", mtxtSettingsSansStartTime.Text.Trim());
                            add_sans.Parameters.AddWithValue("@endTime", mtxtSettingsSansEndTime.Text.Trim());

                            add_sans.ExecuteNonQuery();

                            MessageBox.Show("سانس جدید با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            mtxtSettingsSansStartTime.Clear();
                            mtxtSettingsSansEndTime.Clear();
                            cmbSettingsSansAgeCategory.SelectedIndex = -1;
                            cmbSettingsSansGender.SelectedIndex = -1;

                            cmbSettingsEditSans.Text = "ابتدا سالن را انتخاب کنید";
                            cmbSettingsEditSansGender.Text = "انتخاب سالن";
                            cmbSettingsEditSans.Enabled = false;

                            cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";
                            cmbSettingsSansGender.Text = "انتخاب سالن";

                            cmbSettingsEditSansGender.Focus();

                            InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                            InitializeMainSideDaySans(dgvMainSideSanses);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnAddAgeCategory_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtSettingsAgeCategoryName.Text != "" && txtSettingsAgeCategoryAgeTo.Text != "" && txtSettingsAgeCategoryAgeFrom.Text != "" && Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) >= 1 && Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) <= 100 && Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text) >= 1 && Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text) <= 100 && Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) < Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text))
                {
                    conn.Open();

                    if (btnSettingsAddAgeCategory.Text == "ثبت ویرایش")
                    {
                        SqlCommand com = new SqlCommand("UPDATE tblAgeCategory SET age_category_name = @ageCategoryName, age_from = @ageFrom , age_to = @ageTo WHERE age_category_ID = " + cmbSettingsEditAgeCategory.SelectedValue, conn);

                        com.Parameters.AddWithValue("@ageCategoryName", txtSettingsAgeCategoryName.Text.Trim());
                        com.Parameters.AddWithValue("@ageFrom", txtSettingsAgeCategoryAgeFrom.Text.Trim());
                        com.Parameters.AddWithValue("@ageTo", txtSettingsAgeCategoryAgeTo.Text.Trim());

                        com.ExecuteNonQuery();

                        MessageBox.Show("تغییرات با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                        btnSettingsAddAgeCategory.Text = "افزودن";

                        btnSettingsAgeCategoryRemove.Visible = false;
                        btnSettingsReturnAgeCategory.Visible = false;

                        txtSettingsAgeCategoryAgeFrom.Clear();
                        txtSettingsAgeCategoryAgeTo.Clear();
                        txtSettingsAgeCategoryName.Clear();

                        cmbSettingsEditAgeCategory.Text = "انتخاب گروه سنی";
                        cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";

                        cmbSettingsEditAgeCategory.Focus();

                        InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                        InitializeMainSideDaySans(dgvMainSideSanses);
                    }
                    else
                    {
                        if (txtSettingsAgeCategoryName.Text.Trim() != "" && txtSettingsAgeCategoryAgeFrom.Text.Trim() != "" && txtSettingsAgeCategoryAgeTo.Text.Trim() != "")
                        {
                            SqlCommand add_ageCategory = new SqlCommand("INSERT INTO tblAgeCategory ( age_category_name, age_from , age_to ) Values (@ageCategoryName,@ageFrom , @ageTo)", conn);
                            add_ageCategory.Parameters.AddWithValue("@ageCategoryName", txtSettingsAgeCategoryName.Text.Trim());
                            add_ageCategory.Parameters.AddWithValue("@ageFrom", txtSettingsAgeCategoryAgeFrom.Text.Trim());
                            add_ageCategory.Parameters.AddWithValue("@ageTo", txtSettingsAgeCategoryAgeTo.Text.Trim());

                            add_ageCategory.ExecuteNonQuery();

                            MessageBox.Show("گروه سنی جدید با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            txtSettingsAgeCategoryAgeFrom.Clear();
                            txtSettingsAgeCategoryAgeTo.Clear();
                            txtSettingsAgeCategoryName.Clear();

                            getAgeCategoryData(cmbSettingsEditAgeCategory);
                            getAgeCategoryData(cmbSettingsSansAgeCategory);

                            cmbSettingsEditAgeCategory.Text = "انتخاب گروه سنی";
                            cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";

                            cmbSettingsEditAgeCategory.Focus();

                            InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                            InitializeMainSideDaySans(dgvMainSideSanses);
                        }
                    }

                    conn.Close();
                }
                else if (txtSettingsAgeCategoryName.Text == "")
                    MessageBox.Show("نام گروه سنی خالی است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (txtSettingsAgeCategoryAgeTo.Text == "")
                    MessageBox.Show("قسمت 'از سن' خالی است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (txtSettingsAgeCategoryAgeFrom.Text == "")
                    MessageBox.Show("قسمت 'تا سن' خالی است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (!(Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) >= 1 && Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) <= 100 && Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text) >= 1 && Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text) <= 100 && Convert.ToInt32(txtSettingsAgeCategoryAgeFrom.Text) < Convert.ToInt32(txtSettingsAgeCategoryAgeTo.Text)))
                    MessageBox.Show("رده سنی وارد شده نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegisterRankSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbRegisterRankRank.SelectedValue != null && cmbRegisterRankRank.Text != "ثبت نشده" && cmbRegisterRankAthlete.SelectedValue != null && cmbRegisterSans.SelectedValue != null && CheckDate(mtxtRegisterRankDate))
                {
                    conn.Open();

                    if (cmbRegisterRankRank.Text != lblRegisterRankSideRank.Text)
                    {
                        SqlCommand register_rank = new SqlCommand("INSERT INTO tblAthleteRank ( athlete_ID, rank_ID , rank_date ) Values (@athleteID,@rankID , @Date)", conn);

                        register_rank.Parameters.AddWithValue("@athleteID", cmbRegisterRankAthlete.SelectedValue);
                        register_rank.Parameters.AddWithValue("@rankID", cmbRegisterRankRank.SelectedValue);
                        register_rank.Parameters.AddWithValue("@Date", ConvertDateTime.sh2m(mtxtRegisterRankDate.Text));

                        register_rank.ExecuteNonQuery();
                    }

                    SqlCommand register_sans = new SqlCommand("INSERT INTO tblAthleteSans ( athlete_ID, sans_ID , sans_date ) Values (@athleteID,@sansID , @Date)", conn);

                    register_sans.Parameters.AddWithValue("@athleteID", cmbRegisterRankAthlete.SelectedValue);
                    register_sans.Parameters.AddWithValue("@sansID", cmbRegisterSans.SelectedValue);
                    register_sans.Parameters.AddWithValue("@Date", ConvertDateTime.sh2m(mtxtRegisterRankDate.Text));

                    register_sans.ExecuteNonQuery();

                    conn.Close();

                    MessageBox.Show("ثبت نام با موفقیت انجام شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    cmbRegisterRankAthlete_SelectedIndexChanged(sender, e);
                }
                else if (cmbRegisterRankAthlete.SelectedValue == null)
                    MessageBox.Show("هنرجو نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (cmbRegisterSans.SelectedValue == null)
                    MessageBox.Show("سانس نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (!CheckDate(mtxtRegisterRankDate))
                {
                    MessageBox.Show("تاریخ نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtRegisterRankDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
                else if (cmbRegisterRankRank.SelectedValue == null || cmbRegisterRankRank.Text == "ثبت نشده")
                    MessageBox.Show("سطح نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterRankRank_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbRegisterRankRank.SelectedValue != null)
                {
                    string str_course = "SELECT sections FROM tblRank WHERE rank_ID = " + cmbRegisterRankRank.SelectedValue;

                    SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                    DataTable dt_course = new DataTable();
                    da_course.Fill(dt_course);

                    lblRegisterRankSections.Text = dt_course.Rows[0].ItemArray[0].ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbRegisterRankAthlete_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbRegisterRankAthlete.SelectedValue != null && cmbRegisterRankAthlete.Text != "انتخاب هنرجو")
                {
                    SetSideAthleteDetail(cmbRegisterRankAthlete.SelectedValue, pcbRegisterRankSideAthleteImage, lblRegisterRankSideAthleteID, lblRegisterRankSideFullName, lblRegisterRankSideCellphone, lblRegisterRankSideRank, lblRegisterRankSideCredit);

                    if (lblRegisterRankSideRank.Text != "سطح")
                        cmbRegisterRankRank.Text = lblRegisterRankSideRank.Text;

                    string str_AgeCategory = "SELECT ismale FROM tblAthlete WHERE athlete_ID = " + cmbRegisterRankAthlete.SelectedValue;

                    SqlDataAdapter da_AgeCategory = new SqlDataAdapter(str_AgeCategory, conn);
                    DataTable ds_AgeCategory = new DataTable();
                    da_AgeCategory.Fill(ds_AgeCategory);

                    if (ds_AgeCategory.Rows.Count != 0)
                    {
                        if (!Convert.IsDBNull(ds_AgeCategory.Rows[0].ItemArray[0]))
                        {
                            cmbRegisterSans.Enabled = true;
                            cmbRegisterRankRank.Enabled = true;

                            bool gender = Convert.ToBoolean(ds_AgeCategory.Rows[0].ItemArray[0]);

                            getSansData(gender, cmbRegisterSans);
                        }
                        else
                        {
                            cmbRegisterSans.Enabled = false;
                            cmbRegisterRankRank.Enabled = false;

                            cmbRegisterSans.DataSource = null;
                            cmbRegisterSans.Text = "جنسیت مشخص نیست";
                        }
                    }

                    if (lblRegisterRankSectionsPrice.Text != null)
                        if (char.IsDigit(Convert.ToChar(lblRegisterRankSideCredit.Text.Substring(0, 1))) && Convert.ToInt32(lblRegisterRankSectionsPrice.Text) > 0)
                            lblRegisterRankRemainSections.Text = (Convert.ToInt32(lblRegisterRankSideCredit.Text) / Convert.ToInt32(lblRegisterRankSectionsPrice.Text)).ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnExamSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                string athleteGender = dgvExamAthletes["جنسیت", dgvExamAthletes.CurrentCell.RowIndex].Value.ToString();

                if (dgvExamAthletes.SelectedRows.Count != 0 && txtExamEnglishName.Text != "" && CheckDate(mtxtExamDate) && dgvExamAthletes.CurrentCell != null && cmbExamRank.SelectedValue != null && txtExamPrice.Text != "" && (rdbAccepted.Checked == true || rdbFailed.Checked == true) && athleteGender != "ث.ن")
                {
                    btnPays_Click(sender, e);


                    cmbPayAthlete.SelectedValue = dgvExamAthletes[0, dgvExamAthletes.CurrentCell.RowIndex].Value;
                    txtPayType.Text = "آزمون";

                    txtPayAmount.Text = txtExamPrice.Text;
                    lblPayTotalPrice.Text = txtExamPrice.Text;

                }
                else if (dgvExamAthletes.CurrentCell == null || dgvExamAthletes.SelectedRows.Count == 0)
                    MessageBox.Show("هنرجو نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (txtExamEnglishName.Text == "")
                    MessageBox.Show("نام انگلیسی وارد نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (!CheckDate(mtxtExamDate))
                {
                    MessageBox.Show("تاریخ نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtExamDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
                else if (athleteGender == "ث.ن")
                    MessageBox.Show("جنسیت فرد ثبت نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (cmbExamRank.SelectedValue == null)
                    MessageBox.Show("سطح انتخاب نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (txtExamPrice.Text == "")
                    MessageBox.Show("قیمت آزمون خالی است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (rdbAccepted.Checked != true && rdbFailed.Checked != true)
                    MessageBox.Show("وضعیت قبولی نا مشخص است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbExamRank_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbExamRank.SelectedValue != null)
                {
                    string str_serial = "SELECT TOP(1) exam_serial FROM tblAthleteExam WHERE exam_ID = " + cmbExamRank.SelectedValue + " ORDER BY tblAthleteExam.exam_serial DESC ";

                    SqlDataAdapter da_serial = new SqlDataAdapter(str_serial, conn);
                    DataTable dt_serial = new DataTable();
                    da_serial.Fill(dt_serial);

                    if (dt_serial.Rows.Count != 0)
                    {
                        if (dt_serial.Rows[0].ItemArray[0] != DBNull.Value && Convert.ToInt32(dt_serial.Rows[0].ItemArray[0]) > 100000)
                            txtExamSerial.Text = (Convert.ToInt32(dt_serial.Rows[0].ItemArray[0]) + 1).ToString();
                        else
                            SetExamSerial(cmbExamRank.SelectedValue);
                    }
                    else
                        SetExamSerial(cmbExamRank.SelectedValue);

                    string str_course = "SELECT exam_price FROM tblExam WHERE tblExam.exam_ID = " + cmbExamRank.SelectedValue;

                    SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                    DataTable dt_course = new DataTable();
                    da_course.Fill(dt_course);

                    if (dt_course.Rows.Count != 0)
                        txtExamPrice.Text = dt_course.Rows[0].ItemArray[0].ToString();
                    else
                        txtExamPrice.Clear();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void SetExamSerial(object examID)
        {
            try
            {
                switch (examID)
                {
                    case 0:
                        txtExamSerial.Text = "100000";
                        break;

                    case 1:
                        txtExamSerial.Text = "110000";
                        break;

                    case 2:
                        txtExamSerial.Text = "120000";
                        break;

                    case 3:
                        txtExamSerial.Text = "130000";
                        break;

                    case 4:
                        txtExamSerial.Text = "140000";
                        break;

                    case 5:
                        txtExamSerial.Text = "150000";
                        break;

                    case 6:
                        txtExamSerial.Text = "160000";
                        break;

                    case 7:
                        txtExamSerial.Text = "170000";
                        break;

                    case 8:
                        txtExamSerial.Text = "180000";
                        break;

                    case 9:
                        txtExamSerial.Text = "190000";
                        break;

                    case 10:
                        txtExamSerial.Text = "200000";
                        break;

                    case 11:
                        txtExamSerial.Text = "210000";
                        break;

                    case 12:
                        txtExamSerial.Text = "220000";
                        break;

                    case 13:
                        txtExamSerial.Text = "230000";
                        break;

                    case 14:
                        txtExamSerial.Text = "240000";
                        break;

                    case 15:
                        txtExamSerial.Text = "250000";
                        break;

                    case 16:
                        txtExamSerial.Text = "260000";
                        break;

                    case 17:
                        txtExamSerial.Text = "270000";
                        break;

                    case 18:
                        txtExamSerial.Text = "280000";
                        break;

                    case 19:
                        txtExamSerial.Text = "290000";
                        break;

                    case 20:
                        txtExamSerial.Text = "300000";
                        break;

                    case 21:
                        txtExamSerial.Text = "310000";
                        break;

                    case 22:
                        txtExamSerial.Text = "320000";
                        break;

                    case 23:
                        txtExamSerial.Text = "330000";
                        break;

                    case 24:
                        txtExamSerial.Text = "340000";
                        break;

                    case 25:
                        txtExamSerial.Text = "350000";
                        break;

                    default:
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private static void GetRankColor(Label lbl)
        {
            try
            {
                if (lbl.ForeColor == Color.Transparent)
                    lbl.ForeColor = SystemColors.ControlText;

                switch (lbl.Text)
                {
                    case "کمربند سفید":
                        lbl.BackColor = Color.White;
                        break;

                    case "کمربند زرد":
                        lbl.BackColor = Color.FromArgb(234, 239, 10);
                        break;

                    case "کمربند نارنجی":
                        lbl.BackColor = Color.FromArgb(244, 147, 15);
                        break;

                    case "کمربند سبز":
                        lbl.BackColor = Color.ForestGreen;
                        break;

                    case "کمربند آبی":
                        lbl.BackColor = SystemColors.MenuHighlight;
                        break;

                    case "کمربند بنفش":
                        lbl.BackColor = Color.Purple;
                        break;

                    case "کمربند قهوه‌ای":
                        lbl.BackColor = Color.SaddleBrown;
                        break;

                    case "کمربند مشکی":
                        lbl.BackColor = Color.Black;
                        lbl.ForeColor = Color.Transparent;
                        break;

                    case "سطح یک":
                    case "سطح دو":
                    case "سطح سه":
                    case "سطح چهار":
                    case "سطح پنج":
                    case "سطح شش":
                    case "سطح هفت":
                        lbl.BackColor = Color.RosyBrown;
                        break;

                    case "ثبت نشده":
                        lbl.BackColor = Color.Transparent;
                        lbl.ForeColor = SystemColors.ControlText;
                        break;

                    default:
                        lbl.BackColor = Color.Black;
                        lbl.ForeColor = Color.Transparent;
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void lblRegisterRankSideRank_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (lblRegisterRankSideRank.ForeColor == Color.White)
                    lblRegisterRankSideRank.ForeColor = Color.Black;

                GetRankColor(lblRegisterRankSideRank);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbPayAthlete_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbPayAthlete.SelectedIndex != -1)
                {
                    if (txtPayType.Text == "خرید")
                        SetSideAthleteDetail(cmbPayAthlete.SelectedValue, pcbPaySideAthletePic, lblPaySideID, lblPaySideName, lblPaySideTelephone, lblPaySideRank, lblPaySideCredit, true);
                    else
                        SetSideAthleteDetail(cmbPayAthlete.SelectedValue, pcbPaySideAthletePic, lblPaySideID, lblPaySideName, lblPaySideTelephone, lblPaySideRank, lblPaySideCredit);

                    if (lblPaySideCredit.Text != null)
                        if (char.IsDigit(Convert.ToChar(lblPaySideCredit.Text.Substring(0, 1))) && lblPaySideCredit.Text != "اعتبار" && Convert.ToInt32(lblPaySideCredit.Text) < 0)
                        {
                            txtPayAmount.Text = (-Convert.ToInt32(lblPaySideCredit.Text)).ToString();
                        }
                        else
                            txtPayAmount.Text = "0";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }
        private bool CheckDate(MaskedTextBox mtxt)
        {
            try
            {
                DateTime date = ConvertDateTime.sh2m(mtxt.Text);
                DateTime now = DateTime.Now;

                if (mtxt.Text == "   /    / " || date == default(DateTime) || DateTime.Compare(date, now) > 0)
                    return false;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return true;
        }

        private void btnPayPay_Click(object sender, EventArgs e)
        {
            try
            {

                if ((cmbPayType.SelectedIndex == 0 || cmbPayType.SelectedIndex == 1) && cmbPayAthlete.SelectedValue != null && CheckDate(mtxtPayDate) && txtPayAmount.Text != null && txtPayAmount.Text != "")
                {
                    string str_serial = "SELECT dbo.GetAthleteCredit(" + cmbPayAthlete.SelectedValue + ")";

                    SqlDataAdapter da_serial = new SqlDataAdapter(str_serial, conn);
                    DataTable dt_serial = new DataTable();
                    da_serial.Fill(dt_serial);

                    if (dt_serial.Rows.Count != 0)
                    {
                        int credit = Convert.ToInt32(dt_serial.Rows[0].ItemArray[0]);
                        int maxCredit = 999999999;

                        if (credit + Convert.ToInt32(txtPayAmount.Text) < maxCredit)
                        {

                            conn.Open();

                            DateTime now = ConvertDateTime.sh2m(mtxtPayDate.Text);

                            SqlCommand add_pay = new SqlCommand("INSERT INTO tblAthleteCharge (athlete_ID,athlete_pay,payType_ID,charge_date ) " +
                            "Values (@athleteID,@athletePay,@payTypeID,@ChargeDate)", conn);

                            add_pay.Parameters.AddWithValue("@athleteID", cmbPayAthlete.SelectedValue);
                            add_pay.Parameters.AddWithValue("@athletePay", txtPayAmount.Text);

                            switch (txtPayType.Text)
                            {
                                case "شارژ":
                                    add_pay.Parameters.AddWithValue("@payTypeID", cmbPayType.SelectedIndex == 0 ? 1 : 2);
                                    break;

                                case "پرداخت":
                                    add_pay.Parameters.AddWithValue("@payTypeID", cmbPayType.SelectedIndex == 0 ? 1 : 2);
                                    break;

                                case "فروشگاه":

                                    add_pay.Parameters.AddWithValue("@payTypeID", 2);

                                    string str_newSellFactor = "INSERT INTO [tblSellFactor] " +
                                                               "VALUES (@athlete_ID, @factor_date)";

                                    SqlCommand cmd_newSellFactor = new SqlCommand(str_newSellFactor, conn);
                                    cmd_newSellFactor.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = cmbPayAthlete.SelectedValue;
                                    cmd_newSellFactor.Parameters.Add("@factor_date", SqlDbType.DateTime2).Value = now;

                                    cmd_newSellFactor.ExecuteNonQuery();

                                    for (int i = 0; i < dgvStoreSellShoppingBag.RowCount; i++)
                                    {
                                        string str_newSellOperation = "INSERT INTO [tblSellOperation] " +
                                                                      "VALUES (@sell_factor_ID, @item_specification_ID, @number)";

                                        SqlCommand cmd_newSellOperation = new SqlCommand(str_newSellOperation, conn);
                                        cmd_newSellOperation.Parameters.Add("@sell_factor_ID", SqlDbType.Int).Value = lblStoreSellFactorID.Text;
                                        cmd_newSellOperation.Parameters.Add("@item_specification_ID", SqlDbType.Int).Value = dgvStoreSellShoppingBag["itemSpecificationID", i].Value;
                                        cmd_newSellOperation.Parameters.Add("@number", SqlDbType.SmallInt).Value = dgvStoreSellShoppingBag["itemNum", i].Value;

                                        cmd_newSellOperation.ExecuteNonQuery();
                                    }

                                    DialogResult result = MessageBox.Show("با موفقیت ثبت شد\n\nچاپ فاکتور ؟", "نتیجه", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                    if (result == DialogResult.Yes)
                                        PrintSellFactor();

                                    break;

                                case "آزمون":

                                    SqlCommand add_exam = new SqlCommand("INSERT INTO tblAthleteExam (athlete_ID,exam_ID,exam_serial,exam_date,isaccepted,english_name) " +
                                                                    "Values (@athleteID,@examID,@examSerial,@examDate,@isAccepted,@english_name)", conn);

                                    add_exam.Parameters.AddWithValue("@athleteID", dgvExamAthletes[0, dgvExamAthletes.CurrentCell.RowIndex].Value.ToString());
                                    add_exam.Parameters.AddWithValue("@examID", cmbExamRank.SelectedValue);
                                    add_exam.Parameters.AddWithValue("@examSerial", txtExamSerial.Text);
                                    add_exam.Parameters.AddWithValue("@examDate", ConvertDateTime.sh2m(mtxtExamDate.Text));
                                    add_exam.Parameters.AddWithValue("@isAccepted", rdbAccepted.Checked ? 1 : 0);
                                    add_exam.Parameters.AddWithValue("@english_name", txtExamEnglishName.Text);

                                    add_exam.ExecuteNonQuery();

                                    if (rdbAccepted.Checked == true)
                                    {
                                        string str_course = "SELECT rank_ID FROM tblExam WHERE exam_ID = " + cmbExamRank.SelectedValue;

                                        SqlDataAdapter da_course = new SqlDataAdapter(str_course, conn);
                                        DataTable dt_course = new DataTable();
                                        da_course.Fill(dt_course);

                                        if (dt_course.Rows.Count != 0)
                                        {
                                            SqlCommand register_rank = new SqlCommand("INSERT INTO tblAthleteRank ( athlete_ID, rank_ID , rank_date ) Values (@athleteID,@rankID , @rankDate)", conn);

                                            register_rank.Parameters.AddWithValue("@athleteID", dgvExamAthletes.SelectedCells[0].Value);
                                            register_rank.Parameters.AddWithValue("@rankID", dt_course.Rows[0].ItemArray[0].ToString());
                                            register_rank.Parameters.AddWithValue("@rankDate", ConvertDateTime.sh2m(mtxtExamDate.Text));

                                            register_rank.ExecuteNonQuery();

                                            cmbPayAthlete_SelectedIndexChanged(sender, e);

                                            if (Convert.ToInt32(dt_course.Rows[0].ItemArray[0]) >= 8 && Convert.ToInt32(dt_course.Rows[0].ItemArray[0]) <= 14)
                                                PrintSpecialRanks(ConvertDateTime.sh2m(mtxtExamDate.Text.Substring(0, 2) + " / " + mtxtExamDate.Text.Substring(5, 2) + " / " + mtxtExamDate.Text.Substring(10, 4)), txtExamSerial.Text, txtExamEnglishName.Text, lblExamAthleteFullName.Text, dgvExamAthletes["جنسیت", dgvExamAthletes.CurrentCell.RowIndex].Value.ToString());
                                            else
                                                PrintNormalRanks(ConvertDateTime.sh2m(mtxtExamDate.Text.Substring(0, 2) + " / " + mtxtExamDate.Text.Substring(5, 2) + " / " + mtxtExamDate.Text.Substring(10, 4)), txtExamSerial.Text, txtExamEnglishName.Text, lblExamAthleteFullName.Text, dgvExamAthletes["جنسیت", dgvExamAthletes.CurrentCell.RowIndex].Value.ToString());
                                        }
                                    }
                                    add_pay.Parameters.AddWithValue("@payTypeID", 3);
                                    break;

                                case "ثبت نام":
                                    add_pay.Parameters.AddWithValue("@payTypeID", 4);

                                    break;
                                default:
                                    add_pay.Parameters.AddWithValue("@payTypeID", 1);
                                    break;
                            }

                            add_pay.Parameters.AddWithValue("@ChargeDate", now);

                            add_pay.ExecuteNonQuery();

                            string str_exam;

                            if (txtPayType.Text == "خرید" || cmbPayType.SelectedIndex == 1)
                                str_exam = "SELECT dbo.GetAthleteStoreCredit(" + cmbPayAthlete.SelectedValue + ")";
                            else
                                str_exam = "SELECT dbo.GetAthleteCredit(" + cmbPayAthlete.SelectedValue + ")";

                            SqlDataAdapter da_exam = new SqlDataAdapter(str_exam, conn);
                            DataTable dt_exam = new DataTable();
                            da_exam.Fill(dt_exam);

                            if (dt_exam.Rows.Count != 0)
                                lblPaySideCredit.Text = dt_exam.Rows[0].ItemArray[0].ToString();

                            getCreditColor(lblPaySideCredit);

                            conn.Close();

                            MessageBox.Show("پرداخت با موفقیت انجام شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                            lblPayTotalPrice.Text = "-";

                            txtPayAmount.Text = "0";

                            if (txtPayType.Text != "شارژ")
                            {
                                int lastUser;
                                switch (LastPage)
                                {
                                    case "ثبت نام":
                                        lastUser = cmbRegisterRankAthlete.SelectedIndex;
                                        btnRegisterRank_Click(sender, e);
                                        cmbRegisterRankAthlete.SelectedIndex = lastUser;
                                        break;

                                    case "آزمون":
                                        object lstExamUser = dgvExamAthletes[0, dgvExamAthletes.CurrentCell.RowIndex].Value;
                                        btnExam_Click(sender, e);
                                        SearchDgvSelect(lstExamUser, dgvExamAthletes);
                                        break;

                                    case "فروشگاه":
                                        lastUser = cmbStoreSellAthletesName.SelectedIndex;
                                        btnShop_Click(sender, e);
                                        cmbStoreSellAthletesName.SelectedIndex = lastUser;
                                        break;

                                    case "گزارشات":
                                        object lstReportsUser = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value;
                                        btnReports_Click(sender, e);
                                        SearchDgvSelect(lstReportsUser, dgvReportsAthletes); break;

                                    case "تنظیمات":
                                        btnSettings_Click(sender, e);
                                        break;

                                    case "خانه":
                                        btnHome_Click(sender, e);
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        else
                            MessageBox.Show("اعتبار هنرجو بیشتر از حد مجاز میشود", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }

                else if (cmbPayAthlete.SelectedValue == null)
                    MessageBox.Show("هنرجو نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (!CheckDate(mtxtPayDate))
                {
                    MessageBox.Show("تاریخ نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtPayDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
                else if (txtPayAmount.Text == null || txtPayAmount.Text == "")
                    MessageBox.Show("مبلغ نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (Convert.ToInt32(txtPayAmount.Text) == 0)
                    MessageBox.Show("مبلغ پرداختی صفر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (cmbPayType.SelectedIndex != 0 || cmbPayType.SelectedIndex != 1)
                    MessageBox.Show("نوع اعتبار نا معتبر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("خطا در انجام عملیات", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void ResetExamPage()
        {
            try
            {
                dgvExamAthletes.ClearSelection();
                txtExamEnglishName.ResetText();
                txtExamSearchAthlete.ResetText();
                txtExamPrice.ResetText();
                lblExamAthleteFullName.Text = "نام هنرجو";
                lblExamAthleteID.Text = "کد هنرجو";
                txtExamSerial.Text = "سریال آزمون";
                btnExamPrintLastRank.Text = "آخرین آزمون";

                pcbExamAthletePic.Image = Properties.Resources.background_text;
                cmbExamRank.SelectedValue = -1;
                cmbExamRank.Text = "انتخاب سطح";

                mtxtExamDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);

                rdbAccepted.Checked = false;
                rdbFailed.Checked = false;


            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsAthletesCredit_Click(object sender, EventArgs e)
        {
            try
            {
                if ((dgvReportsAthletes.Rows.Count != 0) && (dgvReportsAthletes.CurrentCell != null))
                {
                    btnPays_Click(sender, e);

                    txtPayType.Text = "پرداخت";

                    cmbPayAthlete.SelectedValue = dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value.ToString();

                    if (Convert.ToInt32(dgvReportsAthletes["اعتبار", dgvReportsAthletes.CurrentCell.RowIndex].Value) < 0)
                    {
                        txtPayAmount.Text = Math.Abs(Convert.ToInt32(dgvReportsAthletes["اعتبار", dgvReportsAthletes.CurrentCell.RowIndex].Value)).ToString();
                        lblPayTotalPrice.Text = txtPayAmount.Text;
                    }

                    else if (Convert.ToInt32(dgvReportsAthletes["اعتبار.ف", dgvReportsAthletes.CurrentCell.RowIndex].Value) < 0)
                    {
                        cmbPayType.SelectedIndex = 1;
                        txtPayAmount.Text = Math.Abs(Convert.ToInt32(dgvReportsAthletes["اعتبار.ف", dgvReportsAthletes.CurrentCell.RowIndex].Value)).ToString();
                        lblPayTotalPrice.Text = txtPayAmount.Text;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsAthletesExam_Click(object sender, EventArgs e)
        {
            try
            {
                if ((dgvReportsAthletes.Rows.Count != 0) && (dgvReportsAthletes.CurrentCell != null))
                {
                    btnExam_Click(sender, e);

                    SearchDgvSelect(dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, dgvExamAthletes);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void SearchDgvSelect(object value, DataGridView dgv)
        {
            try
            {
                dgv.ClearSelection();

                for (int i = 0; i < dgv.RowCount; i++)
                {
                    if (dgv.Rows[i].Cells[0].Value.Equals(value))
                    {
                        dgv.Rows[i].Cells[0].Selected = true;
                        dgv.FirstDisplayedScrollingRowIndex = i;

                        break;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsAthletes_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvReportsPresence.SelectedRows != null)
                    SetSideAthleteDetail(dgvReportsAthletes[0, dgvReportsAthletes.CurrentCell.RowIndex].Value, pcbReportsAthletePic, lblReportsAthleteID, lblReportsAthleteName, lblReportsAthleteCellphone, lblReportsAthleteRank, lblReportsAthleteCredit);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private static void getCreditColor(Label lbl)
        {
            try
            {
                if (Convert.ToInt32(lbl.Text) < 0)
                    lbl.ForeColor = Color.Red;

                else
                    lbl.ForeColor = SystemColors.ControlText;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnRegisterRankCourseCharge_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbRegisterRankAthlete.SelectedValue != null && CheckDate(mtxtRegisterRankDate))
                {
                    btnPays_Click(sender, e);

                    cmbPayAthlete.SelectedValue = cmbRegisterRankAthlete.SelectedValue;

                    txtPayType.Text = "ثبت نام";

                    lblPayTotalPrice.Text = lblRegisterRankCoursePrice.Text;
                    txtPayAmount.Text = lblRegisterRankCoursePrice.Text;
                }
                else if (cmbRegisterRankAthlete.SelectedValue == null)
                    MessageBox.Show("هنرجو نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                else if (!CheckDate(mtxtRegisterRankDate))
                {
                    MessageBox.Show("تاریخ نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    mtxtRegisterRankDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvReportsPresence_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvReportsPresence.SelectedRows != null)
                    SetSideAthleteDetail(dgvReportsPresence[0, dgvReportsPresence.CurrentCell.RowIndex].Value, pcbReportsPresence, lblReportsPresenceAthleteID, lblReportsPresenceName, lblReportsPresenceTell, lblReportsPresenceRank, lblReportsPresenceCredit);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void cmbSettingsEditExam_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string str_ageCategory = "SELECT exam_price FROM tblExam WHERE exam_ID = " + cmbSettingsEditExam.SelectedValue;

                SqlDataAdapter da_ageCategory = new SqlDataAdapter(str_ageCategory, conn);
                DataTable dt_ageCategory = new DataTable();
                da_ageCategory.Fill(dt_ageCategory);

                txtSettingsExamPrice.Text = dt_ageCategory.Rows[0].ItemArray[0].ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterID_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                e.SuppressKeyPress = true;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvExamAthletes_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvExamAthletes.SelectedRows.Count == 1)
                {
                    btnExamPrintLastRank.Text = "آخرین آزمون";
                    btnExamLastExamReturn.Visible = false;

                    string str_presence = "SELECT tblAthlete.athlete_ID, tblName.name_title + ' ' + tblAthlete.l_name, " +
                            "tblAthletePicture.picture_data, (SELECT rankName FROM dbo.GetAthleteRankIdNameDate(tblAthlete.athlete_ID, NULL)) AS 'rank', tblAthlete.ismale " +
                            "FROM tblAthlete LEFT JOIN " +
                            "tblAthletePicture ON tblAthlete.athlete_ID = tblAthletePicture.athlete_ID INNER JOIN " +
                            "tblName ON tblAthlete.name_ID = tblName.name_ID WHERE tblAthlete.athlete_ID = " + dgvExamAthletes[0, dgvExamAthletes.CurrentCell.RowIndex].Value.ToString();

                    SqlDataAdapter da_presence = new SqlDataAdapter(str_presence, conn);
                    DataTable dt_presence = new DataTable();
                    da_presence.Fill(dt_presence);

                    lblExamAthleteID.Text = dt_presence.Rows[0].ItemArray[0].ToString();
                    lblExamAthleteFullName.Text = dt_presence.Rows[0].ItemArray[1].ToString();
                    cmbExamRank.Text = dt_presence.Rows[0].ItemArray[3].ToString();

                    if (Convert.IsDBNull(dt_presence.Rows[0].ItemArray[2]))
                    {
                        object isMale = dt_presence.Rows[0].ItemArray[4];

                        if (Convert.IsDBNull(isMale) || Convert.ToBoolean(isMale))
                            pcbExamAthletePic.Image = Properties.Resources.man__2_;
                        else
                            pcbExamAthletePic.Image = Properties.Resources.woman__2_;
                    }
                    else
                        pcbExamAthletePic.Image = GetImage(dt_presence.Rows[0].ItemArray[2]);

                    cmbExamRank_SelectedIndexChanged(sender, e);

                    txtExamEnglishName.ResetText();
                    mtxtExamDate.Text = ConvertDateTime.m2sh(DateTime.Now).Substring(0, 10);
                    rdbAccepted.Checked = false;
                    rdbFailed.Checked = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void pcbReportsAthletePic_Click(object sender, EventArgs e)
        {
            try
            {
                if (lblReportsAthleteID.Text != "کد")
                    new FPersonInformation(lblReportsAthleteID.Text, lblReportsAthleteRank.BackColor, lblReportsAthleteRank.ForeColor, lblReportsAthleteCredit.ForeColor, dgvMainSideAthletePresence, lblMoneyPerSection, lblReportsAthleteCredit, Height, Width).ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterRankCourseSections_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((!((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift) ||
                    (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) ||
                    e.KeyCode == Keys.Back ||
                    ((e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.Z) && e.Control) ||
                    e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) ||
                    (e.KeyCode == Keys.V && e.Control)))
                {
                    e.SuppressKeyPress = true;
                    txtRegisterRankCourseSections_KeyUp(sender, e);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtRegisterRankCourseSections_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (txtRegisterRankCourseSections.Text.Length <= 4 && txtRegisterRankCourseSections.Text != string.Empty)
                    lblRegisterRankCoursePrice.Text = (Convert.ToInt32(txtRegisterRankCourseSections.Text) * Convert.ToInt32(lblRegisterRankSectionsPrice.Text)).ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsPresenceSearch_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsPresenceStartDate.Text);
                DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsPresenceEndDate.Text);

                string maleOnly = "آقا", femaleOnly = "خانم";

                if (chbReportsPresenceMale.Checked == true)
                    femaleOnly = "آقا";
                else
                    femaleOnly = "خانم";

                if (chbReportsPresenceFemale.Checked == true)
                    maleOnly = "خانم";
                else
                    maleOnly = "آقا";

                string str_presence = "SELECT * " +
                                      "FROM view_athletePresence " +
                                      "WHERE (CONVERT(DATE, [ساعت ورود]) >= '" + startingTime.Date + "' AND CONVERT(DATE, [ساعت ورود]) <= '" + endingTime.Date + "' ) " +
                                      "AND ( جنسیت = N'" + maleOnly + "' OR جنسیت = N'" + femaleOnly + "' OR جنسیت = N'نامشخص' )";

                DataTable dt = DgvSearch(str_presence, "[نام] + CONVERT(NVARCHAR , کد)", txtReportsPresenceSearch.Text);

                if (dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["ساعت ورود"].ToString() != "ث.ن")
                        {
                            string presenceDate = ConvertDateTime.m2sh(Convert.ToDateTime(row["ساعت ورود"]));
                            presenceDate = presenceDate.Substring(11, 2) + ':' + presenceDate.Substring(14, 2) + ':' + presenceDate.Substring(17, 2) +
                                           ' ' +
                                           presenceDate.Substring(6, 4) + '/' + presenceDate.Substring(3, 2) + '/' + presenceDate.Substring(0, 2);
                            row["ساعت ورود"] = presenceDate;
                        }
                    }
                }

                dgvReportsPresence.DataSource = dt;

                lblReportsPresenceTotal.Text = dgvReportsPresence.Rows.Count.ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsAthletesSearch_Click(object sender, EventArgs e)
        {
            try
            {
                int showActiveOnly, maleOnly, femaleOnly;
                string debtorOnly, sans;

                if (cmbReportsAthletesSans.SelectedValue != null)
                {
                    sans = " (SELECT[athlete_ID] FROM [tblAthleteSans] WHERE [sans_ID] = CONVERT(VARCHAR," + cmbReportsAthletesSans.SelectedValue.ToString() + ")) [AS] ON [tblAthlete].[athlete_ID] = [AS].[athlete_ID] INNER JOIN ";
                }
                else
                {
                    sans = null;
                }
                if (chbReportsAthletesOnlyDebtor.Checked == true)
                    debtorOnly = " < ";
                else
                    debtorOnly = " >= ";

                if (chbReportsAthletesDisactiveAthletes.Checked == true)
                    showActiveOnly = 0;
                else
                    showActiveOnly = 1;

                if (chbReportsAthletesMale.Checked == true)
                    femaleOnly = 1;
                else
                    femaleOnly = 0;

                if (chbReportsAthleteFemale.Checked == true)
                    maleOnly = 0;
                else
                    maleOnly = 1;

                string str_presence = "SELECT tblAthlete.athlete_ID AS 'کد', tblName.name_title + ' ' + tblAthlete.l_name AS 'نام', " +
                             "CASE WHEN tblAthlete.birth_date IS NULL THEN N'ث.ن' ELSE CONVERT(VARCHAR,tblAthlete.birth_date) END AS 'تاریخ تولد',CASE WHEN tblAthlete.n_code IS NULL THEN N'ث.ن' WHEN tblAthlete.n_code = '' THEN N'ث.ن' ELSE tblAthlete.n_code END AS 'کد ملی' , " +
                             "CASE WHEN tblAthlete.ismale = 1 THEN N'آقا' WHEN tblAthlete.ismale = 0 THEN N'خانم' ELSE N'ث.ن' END AS 'جنسیت' ," +
                             "CASE WHEN tblAthlete.telephone IS NULL THEN N'ث.ن' ELSE tblAthlete.telephone END AS 'شماره تماس', " +
                             "CASE WHEN dbo.GetAthleteSans(tblAthlete.athlete_ID) IS NULL THEN N'ث.ن' ELSE dbo.GetAthleteSans(tblAthlete.athlete_ID) END AS 'سانس', " +
                             "dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) AS 'اعتبار.ف',dbo.GetAthleteCredit(tblAthlete.athlete_ID) AS 'اعتبار', " +
                             "CONVERT(VARCHAR, tblAthlete.register_date) AS 'تاریخ ثبت نام', " +
                             "CASE WHEN tblAthlete.isActive = 1 THEN N'فعال' WHEN tblAthlete.isActive = 0 THEN N'غیر فعال' ELSE N'ث.ن' END AS 'وضعیت'" +
                             "FROM tblAthlete INNER JOIN " +
                             sans +
                             "tblName ON tblAthlete.name_ID = tblName.name_ID " +
                             "WHERE ( tblAthlete.isactive = 1 OR tblAthlete.isactive = " + showActiveOnly + " ) " +
                             "AND(tblAthlete.ismale = " + maleOnly + " OR tblAthlete.ismale = " + femaleOnly + "  OR tblAthlete.ismale IS NULL ) " +
                             "AND(dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) < 0 OR dbo.GetAthleteStoreCredit(tblAthlete.athlete_ID) " + debtorOnly + " 0 " +
                             "OR dbo.GetAthleteCredit(tblAthlete.athlete_ID) < 0 OR dbo.GetAthleteCredit(tblAthlete.athlete_ID) " + debtorOnly + " 0 ) ";

                DataTable dt = DgvSearch(str_presence, "tblName.name_title + ' ' + tblAthlete.l_name + ' ' + CONVERT(VARCHAR , tblAthlete.athlete_ID)", txtReportsAthletesSearch.Text);

                if (dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["تاریخ تولد"].ToString() != "ث.ن")
                        {
                            string birthDate = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ تولد"]));
                            birthDate = birthDate.Substring(6, 4) + '/' + birthDate.Substring(3, 2) + '/' + birthDate.Substring(0, 2);
                            row["تاریخ تولد"] = birthDate;
                        }

                        string registerDate = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ ثبت نام"]));
                        registerDate = registerDate.Substring(11, 2) + ':' + registerDate.Substring(14, 2) + ':' + registerDate.Substring(17, 2) +
                                       ' ' +
                                       registerDate.Substring(6, 4) + '/' + registerDate.Substring(3, 2) + '/' + registerDate.Substring(0, 2);
                        row["تاریخ ثبت نام"] = registerDate;
                    }
                }

                dgvReportsAthletes.DataSource = dt;

                lblReportsAthletesTotalPersons.Text = dgvReportsAthletes.Rows.Count.ToString();

                for (int i = 0; i < dgvReportsAthletes.RowCount; i++)
                {
                    if ((int)dgvReportsAthletes["اعتبار", i].Value < 0)
                        dgvReportsAthletes["اعتبار", i].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
                for (int j = 0; j < dgvReportsAthletes.RowCount; j++)
                {
                    if ((int)dgvReportsAthletes["اعتبار.ف", j].Value < 0)
                        dgvReportsAthletes["اعتبار.ف", j].Style = new DataGridViewCellStyle { ForeColor = Color.Red };
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsIncomeSearch_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsIncomeStartDate.Text);
                DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsIncomeEndDate.Text);

                string shop = "2", register = "4", exam = "3", charge = "1";
                int maleOnly = 1, femaleOnly = 1;

                if (chbReportsIncomeExam.Checked == true)
                    exam = "3";
                else
                    exam = " NULL ";

                if (chbReportsIncomeRegister.Checked == true)
                    register = "4";
                else
                    register = " NULL ";

                if (chbReportsIncomeShop.Checked == true)
                    shop = "2";
                else
                    shop = " NULL ";

                if (chbReportsIncomeCharge.Checked == true)
                    charge = "1";
                else
                    charge = " NULL ";

                if (chbReportsIncomeFemale.Checked == true)
                    femaleOnly = 0;
                else
                    femaleOnly = 1;

                if (chbReportsIncomeMale.Checked == true)
                    maleOnly = 1;
                else
                    maleOnly = 0;

                if (shop == " NULL " && exam == " NULL " && register == " NULL " && charge == " NULL ")
                {
                    exam = "3";
                    register = "4";
                    shop = "2";
                    charge = "1";
                }

                string str_presence = "SELECT tblAthlete.athlete_ID AS N'کد', tblName.name_title + ' ' + tblAthlete.l_name AS N'نام', " +
                             "CASE WHEN tblAthlete.ismale = 1 THEN N'آقا' WHEN tblAthlete.ismale = 0 THEN N'خانم' ELSE N'ث.ن' END AS N'جنسیت' , CASE WHEN tblAthlete.cellphone IS NULL THEN N'ث.ن' ELSE tblAthlete.cellphone END AS N'شماره تماس', " +
                             "tblPayType.payType_title AS N'بخش', CASE WHEN tblAthleteCharge.athlete_pay > 0 THEN tblAthleteCharge.athlete_pay ELSE '' END AS N'مبلغ پرداختی' , CONVERT(VARCHAR, tblAthleteCharge.charge_date) AS N'تاریخ', dbo.GetAthleteCredit(tblAthlete.athlete_ID) AS N'اعتبار' " +
                             "FROM tblAthlete INNER JOIN " +
                             "tblName ON tblAthlete.name_ID = tblName.name_ID INNER JOIN " +
                             "tblAthleteCharge ON tblAthlete.athlete_ID = tblAthleteCharge.athlete_ID INNER JOIN " +
                             "tblPayType ON tblAthleteCharge.payType_ID = tblPayType.payType_ID " +
                             "WHERE (tblAthlete.ismale = " + maleOnly + " OR tblAthlete.ismale = " + femaleOnly + "  OR tblAthlete.ismale IS NULL) " +
                             "AND (tblAthleteCharge.payType_ID = " + exam + " OR tblAthleteCharge.payType_ID = " + shop + " OR tblAthleteCharge.payType_ID = " + register + " OR tblAthleteCharge.payType_ID = " + charge + ") " +
                             "AND CONVERT(DATE, tblAthleteCharge.charge_date) >= '" + startingTime.Date + "' AND CONVERT(DATE, tblAthleteCharge.charge_date) <= '" + endingTime.Date + "'";

                DataTable dt = DgvSearch(str_presence, "tblName.name_title + ' ' + tblAthlete.l_name + ' ' + CONVERT(VARCHAR , tblAthlete.athlete_ID)", txtReportsIncomeAthlete.Text);

                if (dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["تاریخ"].ToString() != "ث.ن")
                        {
                            string date = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ"]));
                            date = date.Substring(11, 2) + ':' + date.Substring(14, 2) + ':' + date.Substring(17, 2) +
                                   ' ' +
                                   date.Substring(6, 4) + '/' + date.Substring(3, 2) + '/' + date.Substring(0, 2);
                            row["تاریخ"] = date;
                        }
                    }
                }

                dgvReportsIncome.DataSource = dt;

                lblReportsIncomeTotalCourseCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "ثبت نام").ToString();
                lblReportsIncomeTotalExam.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "آزمون").ToString();
                lblReportsIncomeTotalShop.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "فروشگاه").ToString();
                lblReportsIncomeCharge.Text = getDgvColoumnSum(dgvReportsIncome, "مبلغ پرداختی", "بخش", "شارژ").ToString();
                lblReportsIncomeTotalIncome.Text = (Convert.ToInt32(lblReportsIncomeTotalCourseCharge.Text) + Convert.ToInt32(lblReportsIncomeTotalExam.Text) + Convert.ToInt32(lblReportsIncomeTotalShop.Text) + Convert.ToInt32(lblReportsIncomeCharge.Text)).ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnReportsStorageSearch_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime startingTime = ConvertDateTime.sh2m(mtxtReportsStorageStartDate.Text);
                DateTime endingTime = ConvertDateTime.sh2m(mtxtReportsStorageEndDate.Text);

                string str_presence = "SELECT [tblItemSpecification].[item_specification_ID], [tblSellFactor].[sell_factor_ID] AS [فاکتور], " +
                                      "[tblSellFactor].[athlete_ID] AS[کد هنرجو], [tblItem].[item_name] AS[نام کالا], " +
                                      "CASE WHEN[tblColor].[color_name] IS NULL THEN N'ث.ن' ELSE[tblColor].[color_name] END AS[رنگ], " +
                                      "CASE WHEN[tblItemSpecification].[size] IS NULL THEN N'ث.ن' ELSE[tblItemSpecification].[size] END AS[سایز], " +
                                      "CASE WHEN[tblItemSpecification].[ismale] = 1 THEN N'آقایان' WHEN[tblItemSpecification].[ismale] = 0 THEN N'بانوان' ELSE N'ث.ن' END AS[جنسیت], " +
                                      "CONVERT(VARCHAR, [tblSellFactor].[factor_date]) AS[تاریخ فروش], [tblSellOperation].[number] AS[تعداد], " +
                                      "[dbo].[GetItemPrice] ([tblItemSpecification].[item_specification_ID], [tblSellFactor].[factor_date]) AS[قیمت] FROM[tblSellFactor] " +
                                      "INNER JOIN[tblSellOperation] ON[tblSellFactor].[sell_factor_ID] = [tblSellOperation].[sell_factor_ID] " +
                                      "INNER JOIN[tblItemSpecification] ON[tblSellOperation].[item_specification_ID] = [tblItemSpecification].[item_specification_ID] " +
                                      "INNER JOIN[tblItem] ON[tblItemSpecification].[item_ID] = [tblItem].[item_ID] " +
                                      "LEFT JOIN[tblColor] ON[tblItemSpecification].[color_ID] = [tblColor].[color_ID] " +
                                      "WHERE CONVERT(DATE, tblSellFactor.factor_date) >= '" + startingTime + "' AND CONVERT(DATE, tblSellFactor.factor_date) <= '" + endingTime + "'";

                DataTable dt = DgvSearch(str_presence, "tblItem.item_name + ' ' + CONVERT(VARCHAR , tblSellFactor.sell_factor_ID) + ' ' + CONVERT(VARCHAR , tblSellFactor.athlete_ID)", txtReportsStorageSearch.Text);

                if (dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["تاریخ فروش"].ToString() != "ث.ن")
                        {
                            string sellDate = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ فروش"]));
                            sellDate = sellDate.Substring(11, 2) + ':' + sellDate.Substring(14, 2) + ':' + sellDate.Substring(17, 2) +
                                       ' ' +
                                       sellDate.Substring(6, 4) + '/' + sellDate.Substring(3, 2) + '/' + sellDate.Substring(0, 2);
                            row["تاریخ فروش"] = sellDate;
                        }
                    }
                }

                dgvReportsStorage.DataSource = dt;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void pcbReportsAthletePic_MouseLeave(object sender, EventArgs e)
        {
            pcbReportsAthletePic.BackColor = Color.Gray;
        }

        private void pcbReportsAthletePic_MouseEnter(object sender, EventArgs e)
        {
            pcbReportsAthletePic.BackColor = Color.Firebrick;
        }


        private void btnSettingsAgeCategoryRemove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("آیا از حذف گروه سنی اطمینان دارید ؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                if (result == DialogResult.Yes)
                {
                    conn.Open();

                    SqlCommand com = new SqlCommand("DELETE FROM tblAgeCategory WHERE age_category_ID = " + cmbSettingsEditAgeCategory.SelectedValue, conn);

                    com.ExecuteNonQuery();

                    conn.Close();

                    MessageBox.Show("گروه سنی با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    btnSettingsAddAgeCategory.Text = "افزودن";

                    txtSettingsAgeCategoryAgeFrom.Clear();
                    txtSettingsAgeCategoryAgeTo.Clear();
                    txtSettingsAgeCategoryName.Clear();

                    btnSettingsAgeCategoryRemove.Visible = false;
                    btnSettingsReturnAgeCategory.Visible = false;

                    getAgeCategoryData(cmbSettingsEditAgeCategory);
                    getAgeCategoryData(cmbSettingsSansAgeCategory);

                    cmbSettingsEditAgeCategory.Text = "انتخاب گروه سنی";
                    cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";

                    cmbSettingsEditAgeCategory.Focus();

                    InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                    InitializeMainSideDaySans(dgvMainSideSanses);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnSettingsSansRemove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("آیا از حذف سانس اطمینان دارید ؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                if (result == DialogResult.Yes)
                {
                    conn.Open();

                    SqlCommand com = new SqlCommand("DELETE FROM tblSans WHERE sans_ID = " + cmbSettingsEditSans.SelectedValue, conn);

                    com.ExecuteNonQuery();

                    conn.Close();

                    MessageBox.Show("سانس با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

                    btnSettingsAddSans.Text = "افزودن";

                    mtxtSettingsSansStartTime.Clear();
                    mtxtSettingsSansEndTime.Clear();
                    cmbSettingsSansAgeCategory.SelectedIndex = -1;
                    cmbSettingsSansGender.SelectedIndex = -1;

                    btnSettingsReturnSans.Visible = false;
                    btnSettingsSansRemove.Visible = false;

                    cmbSettingsEditSans.Text = "ابتدا سالن را انتخاب کنید";
                    cmbSettingsEditSansGender.Text = "انتخاب سالن";
                    cmbSettingsEditSans.Enabled = false;

                    cmbSettingsSansAgeCategory.Text = "انتخاب گروه سنی";
                    cmbSettingsSansGender.Text = "انتخاب سالن";

                    cmbSettingsEditSansGender.Focus();

                    InitializeMainSideDaySans(dgvMainSideSanses);
                    InitializeMainSideAthletePresence(dgvMainSideAthletePresence, lblMoneyPerSection);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnEditCourse_Click(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(txtSettingsCourseSections.Text) != 0 && Convert.ToInt32(txtSettingsCoursePrice.Text) != 0)
                {
                    DialogResult result = MessageBox.Show("آیا از ویرایش کالا اطمینان دارید؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    if (result == DialogResult.Yes)
                    {
                        conn.Open();

                        SqlCommand com = new SqlCommand("UPDATE tblCharge SET sections = @sections , price  = @price , price_date = @priceDate WHERE charge_ID = 1 ", conn);

                        com.Parameters.AddWithValue("@sections", txtSettingsCourseSections.Text.Trim());
                        com.Parameters.AddWithValue("@price", txtSettingsCoursePrice.Text.Trim());
                        com.Parameters.AddWithValue("@priceDate", System.DateTime.Now);

                        com.ExecuteNonQuery();

                        lblMoneyPerSection.Text = MoneyPerSection().ToString();

                        conn.Close();

                        MessageBox.Show("تغییرات با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }
                else if ((Convert.ToInt32(txtSettingsCourseSections.Text) == 0))
                    MessageBox.Show("تعداد جلسات صفر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (Convert.ToInt32(txtSettingsCoursePrice.Text) == 0)
                    MessageBox.Show("هزینه دوره صفر است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnEditAgeCategory_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbSettingsEditAgeCategory.SelectedValue != null)
                {
                    string str_ageCategory = "SELECT age_category_name , age_from , age_to FROM tblAgeCategory WHERE age_category_ID = " + cmbSettingsEditAgeCategory.SelectedValue;

                    SqlDataAdapter da_ageCategory = new SqlDataAdapter(str_ageCategory, conn);
                    DataTable dt_ageCategory = new DataTable();
                    da_ageCategory.Fill(dt_ageCategory);

                    txtSettingsAgeCategoryName.Text = dt_ageCategory.Rows[0].ItemArray[0].ToString();
                    txtSettingsAgeCategoryAgeFrom.Text = dt_ageCategory.Rows[0].ItemArray[1].ToString();
                    txtSettingsAgeCategoryAgeTo.Text = dt_ageCategory.Rows[0].ItemArray[2].ToString();

                    btnSettingsAddAgeCategory.Text = "ثبت ویرایش";

                    btnSettingsAgeCategoryRemove.Visible = true;
                    btnSettingsReturnAgeCategory.Visible = true;
                }
                else
                    MessageBox.Show("گروه سنی انتخاب نشده است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnEditSans_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbSettingsEditSans.SelectedValue != null)
                {
                    string str_sans = "SELECT CASE WHEN [tblAgeCategory].[age_category_name] IS NOT NULL THEN [tblAgeCategory].[age_category_name] ELSE '' END AS 'AgeCategory', CASE WHEN[tblSans].[ismale] = 1 THEN N'آقایان' ELSE N'بانوان' END AS[isMale], " +
                                                "[tblSans].[start_time] AS 'StartTime', [tblSans].[end_time] AS 'EndTime'" +
                                                "FROM [tblSans] " +
                                                "LEFT JOIN [tblAgeCategory] ON [tblSans].[age_category_ID] = [tblAgeCategory].[age_category_ID] WHERE tblSans.sans_ID = " + cmbSettingsEditSans.SelectedValue;

                    SqlDataAdapter da_sans = new SqlDataAdapter(str_sans, conn);
                    DataTable dt_sans = new DataTable();
                    da_sans.Fill(dt_sans);

                    getAgeCategoryData(cmbSettingsSansAgeCategory);

                    if (dt_sans.Rows.Count != 0)
                    {
                        cmbSettingsSansGender.Text = dt_sans.Rows[0]["isMale"].ToString();
                        cmbSettingsSansAgeCategory.Text = dt_sans.Rows[0]["ageCategory"].ToString();
                        mtxtSettingsSansStartTime.Text = dt_sans.Rows[0]["startTime"].ToString();
                        mtxtSettingsSansEndTime.Text = dt_sans.Rows[0]["endTime"].ToString();

                        btnSettingsAddSans.Text = "ثبت ویرایش";

                        btnSettingsSansRemove.Visible = true;
                        btnSettingsReturnSans.Visible = true;
                    }
                }
                else
                    MessageBox.Show("سانس انتخاب نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnEditExam_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtSettingsExamPrice.Text != "")
                {
                    DialogResult result = MessageBox.Show("آیا از ویرایش کالا اطمینان دارید؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    if (result == DialogResult.Yes)
                    {
                        conn.Open();
                        SqlCommand com = new SqlCommand("UPDATE tblExam SET exam_price  = @examPrice WHERE exam_ID = " + cmbSettingsEditExam.SelectedValue, conn);

                        com.Parameters.AddWithValue("@examPrice", txtSettingsExamPrice.Text.Trim());

                        com.ExecuteNonQuery();
                        conn.Close();

                        MessageBox.Show("تغییرات با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }
                else
                    MessageBox.Show("مبلغ آزمون وارد نشده است", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Print

        private void PrintSellFactor()
        {
            try
            {
                string date = mtxtPayDate.Text.Substring(10, 4) + '/' + mtxtPayDate.Text.Substring(5, 2) + '/' + mtxtPayDate.Text.Substring(0, 2);

                for (int i = 0; i < dgvStoreSellShoppingBag.RowCount; i++)
                {
                    string itemName = dgvStoreSellShoppingBag["itemName", i].Value.ToString();
                    string itemSize = dgvStoreSellShoppingBag["itemSize", i].Value.ToString();
                    string itemColor = dgvStoreSellShoppingBag["itemColor", i].Value.ToString();
                    string itemIsMaleValue = dgvStoreSellShoppingBag["itemIsMaleValue", i].Value.ToString();
                    string itemNumber = dgvStoreSellShoppingBag["itemNum", i].Value.ToString();
                    string itemTotalPrice = dgvStoreSellShoppingBag["itemPrice", i].Value.ToString();
                    string itemPrice = (Convert.ToInt32(itemTotalPrice) / Convert.ToInt32(itemNumber)).ToString();

                    StringBuilder sb_itemDescription = new StringBuilder();

                    sb_itemDescription.Append(itemName);
                    sb_itemDescription.Append(" ");

                    if (itemSize != "")
                    {
                        sb_itemDescription.Append(itemSize);
                        sb_itemDescription.Append(" ");
                    }

                    if (itemColor != "")
                    {
                        sb_itemDescription.Append(itemColor);
                        sb_itemDescription.Append(" ");
                    }

                    if (itemIsMaleValue != "")
                    {
                        sb_itemDescription.Append(itemIsMaleValue);
                        sb_itemDescription.Append(" ");
                    }

                    string str_itemDescription = sb_itemDescription.ToString().TrimEnd();

                    PrintFactor.Load(Application.StartupPath + @"\PrintFactor.mrt");

                    if (i >= 0 && i <= 9)
                    {
                        sellFactorShoppingBag.Add(str_itemDescription);
                        sellFactorShoppingBag.Add(itemNumber);
                        sellFactorShoppingBag.Add(itemPrice);
                        sellFactorShoppingBag.Add(itemTotalPrice);
                    }
                }

                if (dgvStoreSellShoppingBag.RowCount == 1)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];

                }
                else if (dgvStoreSellShoppingBag.RowCount == 2)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 3)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 4)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 5)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 6)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                    PrintFactor.Dictionary.Variables["ItemName6"].Value = sellFactorShoppingBag[20];
                    PrintFactor.Dictionary.Variables["Number6"].Value = sellFactorShoppingBag[21];
                    PrintFactor.Dictionary.Variables["Price6"].Value = sellFactorShoppingBag[22];
                    PrintFactor.Dictionary.Variables["TotalPrice6"].Value = sellFactorShoppingBag[23];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 7)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                    PrintFactor.Dictionary.Variables["ItemName6"].Value = sellFactorShoppingBag[20];
                    PrintFactor.Dictionary.Variables["Number6"].Value = sellFactorShoppingBag[21];
                    PrintFactor.Dictionary.Variables["Price6"].Value = sellFactorShoppingBag[22];
                    PrintFactor.Dictionary.Variables["TotalPrice6"].Value = sellFactorShoppingBag[23];
                    PrintFactor.Dictionary.Variables["ItemName7"].Value = sellFactorShoppingBag[24];
                    PrintFactor.Dictionary.Variables["Number7"].Value = sellFactorShoppingBag[25];
                    PrintFactor.Dictionary.Variables["Price7"].Value = sellFactorShoppingBag[26];
                    PrintFactor.Dictionary.Variables["TotalPrice7"].Value = sellFactorShoppingBag[27];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 8)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                    PrintFactor.Dictionary.Variables["ItemName6"].Value = sellFactorShoppingBag[20];
                    PrintFactor.Dictionary.Variables["Number6"].Value = sellFactorShoppingBag[21];
                    PrintFactor.Dictionary.Variables["Price6"].Value = sellFactorShoppingBag[22];
                    PrintFactor.Dictionary.Variables["TotalPrice6"].Value = sellFactorShoppingBag[23];
                    PrintFactor.Dictionary.Variables["ItemName7"].Value = sellFactorShoppingBag[24];
                    PrintFactor.Dictionary.Variables["Number7"].Value = sellFactorShoppingBag[25];
                    PrintFactor.Dictionary.Variables["Price7"].Value = sellFactorShoppingBag[26];
                    PrintFactor.Dictionary.Variables["TotalPrice7"].Value = sellFactorShoppingBag[27];
                    PrintFactor.Dictionary.Variables["ItemName8"].Value = sellFactorShoppingBag[28];
                    PrintFactor.Dictionary.Variables["Number8"].Value = sellFactorShoppingBag[29];
                    PrintFactor.Dictionary.Variables["Price8"].Value = sellFactorShoppingBag[30];
                    PrintFactor.Dictionary.Variables["TotalPrice8"].Value = sellFactorShoppingBag[31];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 9)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                    PrintFactor.Dictionary.Variables["ItemName6"].Value = sellFactorShoppingBag[20];
                    PrintFactor.Dictionary.Variables["Number6"].Value = sellFactorShoppingBag[21];
                    PrintFactor.Dictionary.Variables["Price6"].Value = sellFactorShoppingBag[22];
                    PrintFactor.Dictionary.Variables["TotalPrice6"].Value = sellFactorShoppingBag[23];
                    PrintFactor.Dictionary.Variables["ItemName7"].Value = sellFactorShoppingBag[24];
                    PrintFactor.Dictionary.Variables["Number7"].Value = sellFactorShoppingBag[25];
                    PrintFactor.Dictionary.Variables["Price7"].Value = sellFactorShoppingBag[26];
                    PrintFactor.Dictionary.Variables["TotalPrice7"].Value = sellFactorShoppingBag[27];
                    PrintFactor.Dictionary.Variables["ItemName8"].Value = sellFactorShoppingBag[28];
                    PrintFactor.Dictionary.Variables["Number8"].Value = sellFactorShoppingBag[29];
                    PrintFactor.Dictionary.Variables["Price8"].Value = sellFactorShoppingBag[30];
                    PrintFactor.Dictionary.Variables["TotalPrice8"].Value = sellFactorShoppingBag[31];
                    PrintFactor.Dictionary.Variables["ItemName9"].Value = sellFactorShoppingBag[32];
                    PrintFactor.Dictionary.Variables["Number9"].Value = sellFactorShoppingBag[33];
                    PrintFactor.Dictionary.Variables["Price9"].Value = sellFactorShoppingBag[34];
                    PrintFactor.Dictionary.Variables["TotalPrice9"].Value = sellFactorShoppingBag[35];
                }
                else if (dgvStoreSellShoppingBag.RowCount == 10)
                {
                    PrintFactor.Dictionary.Variables["CustomerName"].Value = cmbStoreSellAthletesName.Text;
                    PrintFactor.Dictionary.Variables["Date"].Value = date;
                    PrintFactor.Dictionary.Variables["Serialnumber"].Value = lblStoreSellFactorID.Text;
                    PrintFactor.Dictionary.Variables["ItemsTotalPrice"].Value = lblStoreSellItemsTotalPrice.Text;
                    PrintFactor.Dictionary.Variables["ItemName1"].Value = sellFactorShoppingBag[0];
                    PrintFactor.Dictionary.Variables["Number1"].Value = sellFactorShoppingBag[1];
                    PrintFactor.Dictionary.Variables["Price1"].Value = sellFactorShoppingBag[2];
                    PrintFactor.Dictionary.Variables["TotalPrice1"].Value = sellFactorShoppingBag[3];
                    PrintFactor.Dictionary.Variables["ItemName2"].Value = sellFactorShoppingBag[4];
                    PrintFactor.Dictionary.Variables["Number2"].Value = sellFactorShoppingBag[5];
                    PrintFactor.Dictionary.Variables["Price2"].Value = sellFactorShoppingBag[6];
                    PrintFactor.Dictionary.Variables["TotalPrice2"].Value = sellFactorShoppingBag[7];
                    PrintFactor.Dictionary.Variables["ItemName3"].Value = sellFactorShoppingBag[8];
                    PrintFactor.Dictionary.Variables["Number3"].Value = sellFactorShoppingBag[9];
                    PrintFactor.Dictionary.Variables["Price3"].Value = sellFactorShoppingBag[10];
                    PrintFactor.Dictionary.Variables["TotalPrice3"].Value = sellFactorShoppingBag[11];
                    PrintFactor.Dictionary.Variables["ItemName4"].Value = sellFactorShoppingBag[12];
                    PrintFactor.Dictionary.Variables["Number4"].Value = sellFactorShoppingBag[13];
                    PrintFactor.Dictionary.Variables["Price4"].Value = sellFactorShoppingBag[14];
                    PrintFactor.Dictionary.Variables["TotalPrice4"].Value = sellFactorShoppingBag[15];
                    PrintFactor.Dictionary.Variables["ItemName5"].Value = sellFactorShoppingBag[16];
                    PrintFactor.Dictionary.Variables["Number5"].Value = sellFactorShoppingBag[17];
                    PrintFactor.Dictionary.Variables["Price5"].Value = sellFactorShoppingBag[18];
                    PrintFactor.Dictionary.Variables["TotalPrice5"].Value = sellFactorShoppingBag[19];
                    PrintFactor.Dictionary.Variables["ItemName6"].Value = sellFactorShoppingBag[20];
                    PrintFactor.Dictionary.Variables["Number6"].Value = sellFactorShoppingBag[21];
                    PrintFactor.Dictionary.Variables["Price6"].Value = sellFactorShoppingBag[22];
                    PrintFactor.Dictionary.Variables["TotalPrice6"].Value = sellFactorShoppingBag[23];
                    PrintFactor.Dictionary.Variables["ItemName7"].Value = sellFactorShoppingBag[24];
                    PrintFactor.Dictionary.Variables["Number7"].Value = sellFactorShoppingBag[25];
                    PrintFactor.Dictionary.Variables["Price7"].Value = sellFactorShoppingBag[26];
                    PrintFactor.Dictionary.Variables["TotalPrice7"].Value = sellFactorShoppingBag[27];
                    PrintFactor.Dictionary.Variables["ItemName8"].Value = sellFactorShoppingBag[28];
                    PrintFactor.Dictionary.Variables["Number8"].Value = sellFactorShoppingBag[29];
                    PrintFactor.Dictionary.Variables["Price8"].Value = sellFactorShoppingBag[30];
                    PrintFactor.Dictionary.Variables["TotalPrice8"].Value = sellFactorShoppingBag[31];
                    PrintFactor.Dictionary.Variables["ItemName9"].Value = sellFactorShoppingBag[32];
                    PrintFactor.Dictionary.Variables["Number9"].Value = sellFactorShoppingBag[33];
                    PrintFactor.Dictionary.Variables["Price9"].Value = sellFactorShoppingBag[34];
                    PrintFactor.Dictionary.Variables["TotalPrice9"].Value = sellFactorShoppingBag[35];
                    PrintFactor.Dictionary.Variables["ItemName10"].Value = sellFactorShoppingBag[36];
                    PrintFactor.Dictionary.Variables["Number10"].Value = sellFactorShoppingBag[37];
                    PrintFactor.Dictionary.Variables["Price10"].Value = sellFactorShoppingBag[38];
                    PrintFactor.Dictionary.Variables["TotalPrice10"].Value = sellFactorShoppingBag[39];
                }

                PrintFactor.Compile();
                PrintFactor.Show();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public static void PrintNormalRanks(DateTime date, string txtExamSerial, string txtExamEnglishName, string lblExamAthleteFullName, string athleteGender)
        {
            try
            {
                StiReport FirstDoc = new StiReport();
                FirstDoc.Load(Application.StartupPath + @"\BeltDucoment.mrt");

                string shamsiDate = ConvertDateTime.m2sh(date);
                string day = shamsiDate.Substring(0, 2);
                string month = shamsiDate.Substring(3, 2);
                string year = shamsiDate.Substring(6, 4);

                string num = "";

                foreach (char ch in txtExamSerial)
                    if (txtExamSerial != "سریال آزمون")
                        num += (char)(1776 + char.GetNumericValue(ch));

                string temp = "";

                foreach (char ch in day)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                day = temp;
                temp = "";

                foreach (char ch in month)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                month = temp;
                temp = "";

                foreach (char ch in year)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                year = temp;
                temp = "";


                // Persian

                FirstDoc.Dictionary.Variables["Number1"].Value = num;

                FirstDoc.Dictionary.Variables["Date1"].Value = year + "/" + month + "/" + day;

                if (athleteGender == "آقا")
                    FirstDoc.Dictionary.Variables["Name1"].Value = "جناب آقای: " + lblExamAthleteFullName;
                else if (athleteGender == "خانم")
                    FirstDoc.Dictionary.Variables["Name1"].Value = "سرکار خانم: " + lblExamAthleteFullName;
                else
                    FirstDoc.Dictionary.Variables["Name1"].Value = lblExamAthleteFullName;


                // English

                if (txtExamSerial != "سریال آزمون")
                    FirstDoc.Dictionary.Variables["Number2"].Value = txtExamSerial;
                else
                    FirstDoc.Dictionary.Variables["Number2"].Value = "";

                FirstDoc.Dictionary.Variables["Date2"].Value = date.Year.ToString("0000") + '/' + date.Month.ToString("00") + '/' + date.Day.ToString("00");

                if (athleteGender == "آقا")
                    FirstDoc.Dictionary.Variables["Name2"].Value = "Mr: " + txtExamEnglishName;
                else if (athleteGender == "خانم")
                    FirstDoc.Dictionary.Variables["Name2"].Value = "Mrs: " + txtExamEnglishName;
                else
                    FirstDoc.Dictionary.Variables["Name2"].Value = txtExamEnglishName;

                FirstDoc.Compile();
                FirstDoc.Print();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        public static void PrintSpecialRanks(DateTime date, string txtExamSerial, string txtExamEnglishName, string lblExamAthleteFullName, string athleteGender)
        {
            try
            {
                StiReport SecondDoc = new StiReport();
                SecondDoc.Load(Application.StartupPath + @"\DanDucoment.mrt");

                string shamsiDate = ConvertDateTime.m2sh(date);
                string day = shamsiDate.Substring(0, 2);
                string month = shamsiDate.Substring(3, 2);
                string year = shamsiDate.Substring(6, 4);

                string num = "";

                foreach (char ch in txtExamSerial)
                    if (txtExamSerial != "سریال آزمون")
                        num += (char)(1776 + char.GetNumericValue(ch));

                string temp = "";

                foreach (char ch in day)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                day = temp;
                temp = "";

                foreach (char ch in month)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                month = temp;
                temp = "";

                foreach (char ch in year)
                    temp += (char)(1776 + char.GetNumericValue(ch));

                year = temp;
                temp = "";


                // Persian

                SecondDoc.Dictionary.Variables["Number1"].Value = num;

                SecondDoc.Dictionary.Variables["Date1"].Value = year + "/" + month + "/" + day;

                if (athleteGender == "آقا")
                    SecondDoc.Dictionary.Variables["Name1"].Value = "جناب آقای: " + lblExamAthleteFullName;
                else if (athleteGender == "خانم")
                    SecondDoc.Dictionary.Variables["Name1"].Value = "سرکار خانم: " + lblExamAthleteFullName;
                else
                    SecondDoc.Dictionary.Variables["Name1"].Value = lblExamAthleteFullName;


                // English

                if (txtExamSerial != "سریال آزمون")
                    SecondDoc.Dictionary.Variables["Number2"].Value = txtExamSerial;
                else
                    SecondDoc.Dictionary.Variables["Number2"].Value = "";

                SecondDoc.Dictionary.Variables["Date2"].Value = date.Year.ToString("0000") + '/' + date.Month.ToString("00") + '/' + date.Day.ToString("00");

                if (athleteGender == "آقا")
                    SecondDoc.Dictionary.Variables["Name2"].Value = "Mr: " + txtExamEnglishName;
                else if (athleteGender == "خانم")
                    SecondDoc.Dictionary.Variables["Name2"].Value = "Mrs: " + txtExamEnglishName;
                else
                    SecondDoc.Dictionary.Variables["Name2"].Value = txtExamEnglishName;

                SecondDoc.Compile();
                SecondDoc.Print();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

    }
}
