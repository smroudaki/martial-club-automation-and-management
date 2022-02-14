using System;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using Hourado;

namespace StandaloneSDKDemo
{
    public class SDKHelper
    {

        #region Initilizations

        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass();
        private static bool bIsConnected = false;
        private static int iMachineNumber = 1, idwErrorCode = 0;

        private SqlConnection conn;
        private PictureBox pcbRegisterFingerImage;
        private DataGridView dgvMainSideAthletePresence, dgvMainSideSanses;
        private Label moneyPerSection;
        private Button btnConnectDevice;

        private Timer timerEnrollment = new Timer();

        public Timer timerBlinkFingerprintImage = new Timer(), timerFingerprintError = new Timer();
        private Image redFingerprint = Hourado.Properties.Resources.fingerprint__8_;
        private Image greenFingerprint = Hourado.Properties.Resources.fingerprint__5_;
        private Image blueFingerprint = Hourado.Properties.Resources.fingerprint__15_;
        private Image lightBlueFingerprint = Hourado.Properties.Resources.fingerprint__16_;

        public bool enrollmentDone;

        private bool happened, checkEnrollState;
        private int counter = 1;



        #endregion

        #region SDKHelper

        public SDKHelper(SqlConnection conn, PictureBox pcbRegisterFingerImage, DataGridView dgvMainSideAthletePresence, DataGridView dgvMainSideSanses, Label moneyPerSection, Button btnConnectDevice)
        {
            try
            {
                this.conn = conn;
                this.pcbRegisterFingerImage = pcbRegisterFingerImage;
                this.dgvMainSideAthletePresence = dgvMainSideAthletePresence;
                this.dgvMainSideSanses = dgvMainSideSanses;
                this.moneyPerSection = moneyPerSection;
                this.btnConnectDevice = btnConnectDevice;

                timerEnrollment.Tick += TimerEnrollment_Tick;
                timerEnrollment.Interval = 2500;

                timerBlinkFingerprintImage.Tick += TimerBlinkFingerprintImage_Tick;
                timerBlinkFingerprintImage.Interval = 250;

                timerFingerprintError.Tick += TimerFingerprintError_Tick;
                timerFingerprintError.Interval = 2000;
            }
            catch (Exception)
            {
                MessageBox.Show("تنظیمات اس دی کی ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        #endregion

        #region Timers Tick

        private void TimerEnrollment_Tick(object sender, EventArgs e)
        {
            try
            {
                timerEnrollment.Stop();

                if (!checkEnrollState && timerBlinkFingerprintImage.Enabled)
                {
                    timerBlinkFingerprintImage.Stop();
                    pcbRegisterFingerImage.Image = redFingerprint;

                    timerFingerprintError.Start();
                }
            }
            catch (Exception)
            {

            }
        }

        private void TimerFingerprintError_Tick(object sender, EventArgs e)
        {
            try
            {
                timerBlinkFingerprintImage.Start();
                timerFingerprintError.Stop();
            }
            catch (Exception)
            {

            }
        }

        private void TimerBlinkFingerprintImage_Tick(object sender, EventArgs e)
        {
            try
            {
                if (pcbRegisterFingerImage.Image == blueFingerprint)
                    pcbRegisterFingerImage.Image = lightBlueFingerprint;
                else
                    pcbRegisterFingerImage.Image = blueFingerprint;
            }
            catch (Exception)
            {

            }
        }



        #endregion

        #region UserBioTypeClass

        public SupportBiometricType supportBiometricType { get; } = new SupportBiometricType();

        public string biometricType { get; private set; } = string.Empty;

        public class SupportBiometricType
        {
            public bool fp_available { get; set; }
            public bool face_available { get; set; }
            public bool fingerVein_available { get; set; }
            public bool palm_available { get; set; }
        }



        #endregion

        #region ConnectDevice

        public bool GetConnectState()
        {
            return bIsConnected;
        }

        public void SetConnectState(bool state)
        {
            bIsConnected = state;
        }

        public int GetMachineNumber()
        {
            return iMachineNumber;
        }

        public int sta_ConnectTCP(string ip, string port, string commKey)
        {
            if (Convert.ToInt32(port) <= 0 || Convert.ToInt32(port) > 65535 ||
                Convert.ToInt32(commKey) < 0 || Convert.ToInt32(commKey) > 999999)
                return -1;

            axCZKEM1.SetCommPassword(Convert.ToInt32(commKey));

            if (GetConnectState())
            {
                axCZKEM1.Disconnect();
                SetConnectState(false);

                sta_UnRegRealTime();

                return -2;
            }

            int idwErrorCode = 0;

            if (axCZKEM1.Connect_Net(ip, Convert.ToInt32(port)))
            {
                SetConnectState(true);

                sta_RegRealTime();
                sta_getBiometricType();

                return 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);
                return idwErrorCode;
            }
        }

        public void sta_DisConnect()
        {
            if (GetConnectState())
            {
                axCZKEM1.Disconnect();
                SetConnectState(false);

                sta_UnRegRealTime();
            }
        }



        #endregion

        #region DeviceOperations

        public int sta_SYNCTime()
        {
            if (axCZKEM1.SetDeviceTime(iMachineNumber))
            {
                axCZKEM1.RefreshData(iMachineNumber);
                return 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);

                MessageBox.Show("تنظیم ساعت دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                return -1;
            }
        }

        public int sta_btnRestartDevice()
        {
            if (axCZKEM1.RestartDevice(iMachineNumber))
            {
                sta_DisConnect();
                return 1;
            }

            MessageBox.Show("استارت دوباره دستگاه مقدور نیست\nدوباره سعی کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            return -1;
        }

        public int sta_btnPowerOffDevice()
        {
            if (axCZKEM1.PowerOffDevice(iMachineNumber))
            {
                sta_DisConnect();
                return 1;
            }

            MessageBox.Show("خاموش کردن دستگاه مقدور نیست\nدوباره سعی کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            return -1;
        }



        #endregion

        #region RealTimeEvent

        public void sta_UnRegRealTime()
        {
            axCZKEM1.OnAttTransactionEx -= new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler(axCZKEM1_OnAttTransactionEx);
            axCZKEM1.OnFingerFeature -= new zkemkeeper._IZKEMEvents_OnFingerFeatureEventHandler(axCZKEM1_OnFingerFeature);
            axCZKEM1.OnEnrollFingerEx -= new zkemkeeper._IZKEMEvents_OnEnrollFingerExEventHandler(axCZKEM1_OnEnrollFingerEx);
        }

        public int sta_RegRealTime()
        {
            if (!GetConnectState())
                return -1024;

            int ret = 0;

            if (axCZKEM1.RegEvent(GetMachineNumber(), 65535))
            {
                axCZKEM1.OnFingerFeature += new zkemkeeper._IZKEMEvents_OnFingerFeatureEventHandler(axCZKEM1_OnFingerFeature);
                axCZKEM1.OnAttTransactionEx += new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler(axCZKEM1_OnAttTransactionEx);
                axCZKEM1.OnEnrollFingerEx += new zkemkeeper._IZKEMEvents_OnEnrollFingerExEventHandler(axCZKEM1_OnEnrollFingerEx);

                ret = 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);
                ret = idwErrorCode;
            }

            return ret;
        }

        void axCZKEM1_OnEnrollFingerEx(string EnrollNumber, int FingerIndex, int ActionResult, int TemplateLength)
        {
            try
            {
                if (ActionResult == 0 && !happened)
                {
                    enrollmentDone = happened = checkEnrollState = true;

                    timerBlinkFingerprintImage.Stop();
                    pcbRegisterFingerImage.Image = greenFingerprint;
                }
                else if (ActionResult != 0)
                {
                    counter = 1;

                    timerBlinkFingerprintImage.Stop();
                    pcbRegisterFingerImage.Image = blueFingerprint;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("ثبت نهایی اثر انگشت در دستگاه ناموفق\nدوباره سعی کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        void axCZKEM1_OnFingerFeature(int Score)
        {
            try
            {
                if (counter == 3)
                {
                    counter = 0;
                    checkEnrollState = happened = false;

                    timerEnrollment.Start();
                }

                counter++;
            }
            catch (Exception)
            {
                MessageBox.Show("ثبت اثر انگشت در دستگاه ناموفق\nدوباره سعی کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        void axCZKEM1_OnAttTransactionEx(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            PresenceTransaction(Convert.ToInt32(EnrollNumber), AttState, DateTime.Now);
        }

        private int GetNextSans(int sansID, bool salonID)
        {
            try
            {
                string str_getNextSans = "SELECT [dbo].[GetNextSans](@sansID, @salonID)";

                SqlCommand cmd_getNextSans = new SqlCommand(str_getNextSans, conn);
                cmd_getNextSans.Parameters.Add("@sansID", SqlDbType.Int).Value = sansID;
                cmd_getNextSans.Parameters.Add("@salonID", SqlDbType.Bit).Value = salonID;

                SqlDataAdapter adp_getNextSans = new SqlDataAdapter(cmd_getNextSans);
                DataTable dt_getNextSans = new DataTable();
                adp_getNextSans.Fill(dt_getNextSans);

                if (!Convert.IsDBNull(dt_getNextSans.Rows[0][0]))
                    return Convert.ToInt32(dt_getNextSans.Rows[0][0]);
            }
            catch (Exception)
            {
                MessageBox.Show("دریافت سانس بعدی ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        public int PresenceTransaction(int EnrollNumber, int AttState, DateTime dt)
        {
            try
            {
                if (AttState >= 1 && AttState <= 3)
                {
                    string str_getAthlete = "SELECT 1 " +
                                            "FROM [tblAthlete] " +
                                            "WHERE [athlete_ID] = @athlete_ID";

                    SqlCommand cmd_getAthlete = new SqlCommand(str_getAthlete, conn);
                    cmd_getAthlete.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;

                    SqlDataAdapter adp_getAthlete = new SqlDataAdapter(cmd_getAthlete);
                    DataTable dt_getAthlete = new DataTable();
                    adp_getAthlete.Fill(dt_getAthlete);

                    if (dt_getAthlete.Rows.Count > 0)
                    {
                        string str_checkPresence = "SELECT 1 " +
                                                   "FROM [tblAthletePresence] " +
                                                   "WHERE [athlete_ID] = @athlete_ID AND" +
                                                         "[presence_date] = @presence_date";

                        SqlCommand cmd_checkPresence = new SqlCommand(str_checkPresence, conn);
                        cmd_checkPresence.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;
                        cmd_checkPresence.Parameters.Add("@presence_date", SqlDbType.DateTime2).Value = dt;

                        SqlDataAdapter adp_checkPresence = new SqlDataAdapter(cmd_checkPresence);
                        DataTable dt_checkPresence = new DataTable();
                        adp_checkPresence.Fill(dt_checkPresence);

                        if (dt_checkPresence.Rows.Count == 0)
                        {
                            string str_getAthleteRank = "SELECT [rankID] " +
                                                        "FROM [dbo].[GetAthleteRankIdNameDate](@athleteID, NULL)";

                            SqlCommand cmd_getAthleteRank = new SqlCommand(str_getAthleteRank, conn);
                            cmd_getAthleteRank.Parameters.Add("@athleteID", SqlDbType.Int).Value = EnrollNumber;

                            SqlDataAdapter adp_getAthleteRank = new SqlDataAdapter(cmd_getAthleteRank);
                            DataTable dt_getAthleteRank = new DataTable();
                            adp_getAthleteRank.Fill(dt_getAthleteRank);

                            if (dt_getAthleteRank.Rows.Count > 0 && (int)dt_getAthleteRank.Rows[0][0] != -1)
                            {
                                string str_getAthleteSalonID = "SELECT [ismale] " +
                                                               "FROM [tblAthlete] " +
                                                               "WHERE [athlete_ID] = @athlete_ID";

                                SqlCommand cmd_getAthleteSalonID = new SqlCommand(str_getAthleteSalonID, conn);
                                cmd_getAthleteSalonID.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;

                                SqlDataAdapter adp_getAthleteSalonID = new SqlDataAdapter(cmd_getAthleteSalonID);
                                DataTable dt_getAthleteSalonID = new DataTable();
                                adp_getAthleteSalonID.Fill(dt_getAthleteSalonID);

                                if (dt_getAthleteSalonID.Rows.Count > 0)
                                {
                                    object salonID = Convert.IsDBNull(dt_getAthleteSalonID.Rows[0][0]) ? DBNull.Value : dt_getAthleteSalonID.Rows[0][0];

                                    string str_getAthletePresenceDetail = "SELECT [tblName].[name_title] + ' ' + [tblAthlete].[l_name] AS [fullName], " +
                                                                          "       [tblAthletePicture].[picture_data], [dbo].[GetAthleteCredit](@athlete_ID) AS [credit], " +
                                                                          "       [sansIdAgeCategoryIdName].[ageCategoryName], [sansIdAgeCategoryIdName].[ageCategoryID], " +
                                                                          "       [sansIdAgeCategoryIdName].[sansID], " +
                                                                          "       [dbo].[GetAthleteSessionsPassed](@athlete_ID, NULL, NULL) AS [sessionsPassed]," +
                                                                          "       [tblAthlete].[isactive] " +
                                                                          "FROM [tblName] " +
                                                                          "INNER JOIN [tblAthlete] ON [tblName].[name_ID] = [tblAthlete].[name_ID] " +
                                                                          "LEFT JOIN [tblAthletePicture] ON [tblAthlete].[athlete_ID] = [tblAthletePicture].[athlete_ID], " +
                                                                          "[dbo].[GetSansIdAgeCategoryIdName](@salonID, @time) AS [sansIdAgeCategoryIdName] " +
                                                                          "WHERE [tblAthlete].[athlete_ID] = @athlete_ID";

                                    SqlCommand cmd_getAthletePresenceDetail = new SqlCommand(str_getAthletePresenceDetail, conn);
                                    cmd_getAthletePresenceDetail.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;
                                    cmd_getAthletePresenceDetail.Parameters.Add("@salonID", SqlDbType.Bit).Value = salonID;
                                    cmd_getAthletePresenceDetail.Parameters.Add("@time", SqlDbType.Time).Value = dt.TimeOfDay;

                                    SqlDataAdapter adp_getAthletePresenceDetail = new SqlDataAdapter(cmd_getAthletePresenceDetail);
                                    DataTable dt_getAthletePresenceDetail = new DataTable();
                                    adp_getAthletePresenceDetail.Fill(dt_getAthletePresenceDetail);

                                    if (dt_getAthletePresenceDetail.Rows.Count > 0)
                                    {
                                        int athleteSans = Convert.ToInt32(dt_getAthletePresenceDetail.Rows[0]["sansID"]);

                                        if (athleteSans == -1 || athleteSans == -2 || athleteSans == -3 || athleteSans == -4)
                                        {
                                            MessageBox.Show("سانس فعالی موجود نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                            return -1;
                                        }

                                        if (athleteSans == -5)
                                        {
                                            MessageBox.Show("جنسیت هنرجو به منظور تعیین سالن ثبت نشده", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                                            return -2;
                                        }

                                        if (AttState > 1)
                                        {
                                            if (AttState == 2)
                                            {
                                                if (GetNextSans(athleteSans, (bool)salonID) == -1)
                                                    AttState = 1;
                                            }
                                            else if (AttState == 3)
                                            {
                                                int nextSans = GetNextSans(athleteSans, (bool)salonID);

                                                if (nextSans != -1)
                                                {
                                                    if (GetNextSans(nextSans, (bool)salonID) == -1)
                                                        AttState = 2;
                                                }
                                                else
                                                    AttState = 1;
                                            }
                                        }

                                        string str_newAthletePresence = "INSERT INTO tblAthletePresence " +
                                                                        "VALUES (@athlete_ID, @sans_ID, @presence_date, @sections_num)";

                                        SqlCommand cmd_newAthletePresence = new SqlCommand(str_newAthletePresence, conn);
                                        cmd_newAthletePresence.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;
                                        cmd_newAthletePresence.Parameters.Add("@sans_ID", SqlDbType.Int).Value = athleteSans;
                                        cmd_newAthletePresence.Parameters.Add("@presence_date", SqlDbType.DateTime2).Value = dt;
                                        cmd_newAthletePresence.Parameters.Add("@sections_num", SqlDbType.TinyInt).Value = Convert.ToByte(AttState);

                                        conn.Open();
                                        cmd_newAthletePresence.ExecuteNonQuery();

                                        int athletePay = -Convert.ToInt32(moneyPerSection.Text) * AttState;

                                        string str_newAthleteCharge = "INSERT INTO [tblAthleteCharge] " +
                                                                      "VALUES (@athlete_ID, 1, @athlete_pay, @charge_date)";

                                        SqlCommand cmd_newAthleteCharge = new SqlCommand(str_newAthleteCharge, conn);
                                        cmd_newAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;
                                        cmd_newAthleteCharge.Parameters.Add("@athlete_pay", SqlDbType.Int).Value = athletePay;
                                        cmd_newAthleteCharge.Parameters.Add("@charge_date", SqlDbType.DateTime2).Value = dt;

                                        cmd_newAthleteCharge.ExecuteNonQuery();

                                        if (!(bool)dt_getAthletePresenceDetail.Rows[0]["isactive"])
                                        {
                                            string str_updateIsActiveState = "UPDATE [tblAthlete] " +
                                                                             "SET [isactive] = @isactive " +
                                                                             "WHERE [athlete_ID] = @athlete_ID";

                                            SqlCommand cmd_updateIsActiveState = new SqlCommand(str_updateIsActiveState, conn);
                                            cmd_updateIsActiveState.Parameters.Add("@isactive", SqlDbType.Bit).Value = true;
                                            cmd_updateIsActiveState.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = EnrollNumber;

                                            cmd_updateIsActiveState.ExecuteNonQuery();
                                        }

                                        conn.Close();

                                        Image athletePic;

                                        if (Convert.IsDBNull(dt_getAthletePresenceDetail.Rows[0]["picture_data"]))
                                        {
                                            if (Convert.IsDBNull(salonID) || Convert.ToBoolean(salonID))
                                                athletePic = Hourado.Properties.Resources.man__2_;
                                            else
                                                athletePic = Hourado.Properties.Resources.woman__2_;
                                        }
                                        else
                                            athletePic = FMainForm.GetImage(dt_getAthletePresenceDetail.Rows[0]["picture_data"]);

                                        object athleteName = dt_getAthletePresenceDetail.Rows[0]["fullName"];
                                        object athleteCredit = (int)dt_getAthletePresenceDetail.Rows[0]["credit"] + athletePay;
                                        object athleteSessionsPassed = (int)dt_getAthletePresenceDetail.Rows[0]["sessionsPassed"] + AttState;
                                        object athleteAgeCategory = dt_getAthletePresenceDetail.Rows[0]["ageCategoryName"];

                                        if (dgvMainSideAthletePresence.RowCount == 10)
                                        {
                                            dgvMainSideAthletePresence.Rows.RemoveAt(dgvMainSideAthletePresence.RowCount - 1);
                                            dgvMainSideAthletePresence.Rows.Add((int)dgvMainSideAthletePresence["rowID", 0].Value + 1, EnrollNumber, athleteName, athleteCredit, athleteSessionsPassed, athleteAgeCategory, AttState, athletePic);
                                        }
                                        else
                                            dgvMainSideAthletePresence.Rows.Add(dgvMainSideAthletePresence.RowCount + 1, EnrollNumber, athleteName, athleteCredit, athleteSessionsPassed, athleteAgeCategory, AttState, athletePic);

                                        dgvMainSideAthletePresence.Sort(dgvMainSideAthletePresence.Columns["rowID"], System.ComponentModel.ListSortDirection.Descending);
                                        dgvMainSideAthletePresence.CurrentCell = dgvMainSideAthletePresence["athleteID", 0];

                                        if ((int)dgvMainSideAthletePresence["athleteCredit", 0].Value < 0)
                                            dgvMainSideAthletePresence["athleteCredit", 0].Style = new DataGridViewCellStyle { ForeColor = Color.Red };

                                        FMainForm.InitializeMainSideDaySans(dgvMainSideSanses);

                                        return 1;
                                    }
                                }
                            }
                            else
                                MessageBox.Show("فاقد سطح", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        }
                        else
                            MessageBox.Show("حضور تکراری است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                    else
                        MessageBox.Show("کاربر در سیستم ثبت نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                }
                else
                    MessageBox.Show("تعداد جلسه حضور تعریف نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("ثبت حضور ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -3;
        }



        #endregion

        #region DataMng

        #region ClearData

        public int sta_DeleteAttLog()
        {
            int ret = 0;

            axCZKEM1.EnableDevice(GetMachineNumber(), false);

            if (axCZKEM1.ClearGLog(GetMachineNumber()))
            {
                axCZKEM1.RefreshData(GetMachineNumber());
                ret = 1;
            }
            else
            {
                axCZKEM1.GetLastError(ref idwErrorCode);
                ret = idwErrorCode;

                if (idwErrorCode != 0)
                    MessageBox.Show("حذف رکورد های دستگاه مقدور نیست", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            axCZKEM1.EnableDevice(GetMachineNumber(), true);

            return ret;
        }



        #endregion



        #endregion

        #region UserMng

        #region UserInfo

        public int sta_GetUserInfo(int userID)
        {
            string sUserID = userID.ToString().Trim();

            if (sUserID == "")
                return -1023;

            int iPIN2Width = 0;
            string strTemp = "";

            axCZKEM1.GetSysOption(GetMachineNumber(), "~PIN2Width", out strTemp);
            iPIN2Width = Convert.ToInt32(strTemp);

            if (sUserID.Length > iPIN2Width)
                return -1022;

            int iPrivilege = 0;
            string strName = "", strPassword = "";
            bool bEnabled = false;

            axCZKEM1.EnableDevice(iMachineNumber, false);

            if (axCZKEM1.SSR_GetUserInfo(iMachineNumber, sUserID, out strName, out strPassword, out iPrivilege, out bEnabled))
            {
                axCZKEM1.EnableDevice(iMachineNumber, true);
                return 1;
            }
            else
            {
                axCZKEM1.EnableDevice(iMachineNumber, true);
                return -1;
            }
        }

        public int sta_DeleteEnrollData(int userID, int backupNumber)
        {
            if (axCZKEM1.SSR_DeleteEnrollData(iMachineNumber, userID.ToString().Trim(), backupNumber))
            {
                axCZKEM1.RefreshData(iMachineNumber);
                return 1;
            }

            return -1;
        }

        public int sta_OnlineEnroll(int userId, int fingerIndex, int flag)
        {
            int iPIN2Width = 0, iIsABCPinEnable = 0, iT9FunOn = 0;
            string strTemp = "";

            axCZKEM1.GetSysOption(GetMachineNumber(), "~PIN2Width", out strTemp);
            iPIN2Width = Convert.ToInt32(strTemp);
            axCZKEM1.GetSysOption(GetMachineNumber(), "~IsABCPinEnable", out strTemp);
            iIsABCPinEnable = Convert.ToInt32(strTemp);
            axCZKEM1.GetSysOption(GetMachineNumber(), "~T9FunOn", out strTemp);
            iT9FunOn = Convert.ToInt32(strTemp);

            string sUserID = userId.ToString();

            if (sUserID.Length > iPIN2Width)
                return -1022;

            if (iIsABCPinEnable == 0 || iT9FunOn == 0)
            {
                if (sUserID.Substring(0, 1) == "0")
                    return -1022;

                foreach (char tempchar in sUserID.ToCharArray())
                {
                    if (!(char.IsDigit(tempchar)))
                        return -1022;
                }
            }

            sUserID = sUserID.Trim();

            axCZKEM1.CancelOperation();

            if (sta_GetUserInfo(userId) == 1)
            {
                sta_DeleteEnrollData(userId, 12);

                sta_DisConnect();
                if (sta_ConnectTCP(FMainForm.IP, FMainForm.PORT, FMainForm.COMMKEY) != 1)
                {
                    MessageBox.Show("اتصال کابل شبکه را چک کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    btnConnectDevice.BackgroundImage = Hourado.Properties.Resources.wifi_signal_waves__1_;

                    return -1;
                }

                MessageBox.Show("اثر انگشت ثبت شده پاک شد\n... ثبت اثر انگشت جدید", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            axCZKEM1.SSR_DelUserTmpExt(iMachineNumber, sUserID, fingerIndex);

            if (axCZKEM1.StartEnrollEx(sUserID, fingerIndex, flag))
            {
                if (!timerBlinkFingerprintImage.Enabled)
                {
                    pcbRegisterFingerImage.Image = blueFingerprint;
                    timerBlinkFingerprintImage.Start();
                }
            }
            else
                MessageBox.Show("ثبت اثر انگشت مقدور نیست\nدوباره سعی کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);

            return 1;
        }

        public int sta_SetUserInfo(string txtUserID, string txtName, int privilege = 0, string password = "")
        {
            if (privilege >= 5)
                return -1023;

            int iPIN2Width = 0, iIsABCPinEnable = 0, iT9FunOn = 0;
            string strTemp = "";

            axCZKEM1.GetSysOption(GetMachineNumber(), "~PIN2Width", out strTemp);
            iPIN2Width = Convert.ToInt32(strTemp);
            axCZKEM1.GetSysOption(GetMachineNumber(), "~IsABCPinEnable", out strTemp);
            iIsABCPinEnable = Convert.ToInt32(strTemp);
            axCZKEM1.GetSysOption(GetMachineNumber(), "~T9FunOn", out strTemp);
            iT9FunOn = Convert.ToInt32(strTemp);

            if (txtUserID.Length > iPIN2Width)
                return -1022;

            if (iIsABCPinEnable == 0 || iT9FunOn == 0)
            {
                if (txtUserID.Substring(0, 1) == "0")
                    return -1022;

                foreach (char tempchar in txtUserID.ToCharArray())
                {
                    if (!(char.IsDigit(tempchar)))
                        return -1022;
                }
            }

            string sdwEnrollNumber = txtUserID.Trim();

            axCZKEM1.EnableDevice(iMachineNumber, false);

            if (!axCZKEM1.SSR_SetUserInfo(iMachineNumber, sdwEnrollNumber, txtName.Trim(), password, privilege, true))
                return 3;

            axCZKEM1.RefreshData(iMachineNumber);
            axCZKEM1.EnableDevice(iMachineNumber, true);

            return 1;
        }



        #endregion

        #region UserBio

        private string sta_getSysOptions(string option)
        {
            string value = string.Empty;
            axCZKEM1.GetSysOption(iMachineNumber, option, out value);
            return value;
        }

        public void sta_getBiometricType()
        {
            string result = sta_getSysOptions("BiometricType");

            if (!string.IsNullOrEmpty(result))
            {
                supportBiometricType.fp_available = result[1] == '1';
                supportBiometricType.face_available = result[2] == '1';

                if (result.Length >= 9)
                {
                    supportBiometricType.fingerVein_available = result[7] == '1';
                    supportBiometricType.palm_available = result[8] == '1';
                }
            }

            biometricType = result;
        }



        #endregion



        #endregion

    }
}
