using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using StandaloneSDKDemo;

namespace Hourado
{
    public partial class FPersonInformation : Form
    {
        // -------------------- Initilizations --------------------

        private SqlConnection conn = FMainForm.conn;

        private SDKHelper sdk = FMainForm.sdk;

        private DataGridView dgvMainSideAthletePresence;
        private Label moneyPerSection, lblReportsAthleteCredit;

        private int rankID;
        private DateTime examDate;
        private string englishName;

        private bool presenceFirstDateOrder = true, paymentFirstDateOrder = true;
        private byte presenceFirstDateOrderTemp = 0, paymentFirstDateOrderTemp = 2;



        // -------------------- FPersonInformation --------------------

        public FPersonInformation(string id, Color rankBackColor, Color rankForeColor, Color creditForeColor, DataGridView dgvMainSideAthletePresence, Label moneyPerSection, Label lblReportsAthleteCredit, int height, int width)
        {
            try
            {
                InitializeComponent();

                this.dgvMainSideAthletePresence = dgvMainSideAthletePresence;
                this.moneyPerSection = moneyPerSection;
                this.lblReportsAthleteCredit = lblReportsAthleteCredit;

                FMainForm.SetSideAthleteDetail(id, pcbPersonalImage, lblPersonalID, lblPersonalName, lblPersonalNumber, lblPersonalRank, lblPersonalCredit);

                lblPersonalRank.BackColor = rankBackColor;
                lblPersonalRank.ForeColor = rankForeColor;
                lblPersonalCredit.ForeColor = creditForeColor;

                Width = width - (width / 4);
                Height = height - (height / 5);

            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void PersonInformation_Load(object sender, EventArgs e)
        {
            try
            {
                LoadExamData();

                FillDgvPayment("DESC");
                IncludeDgvPersonalPaymentDeleteLinks();

                FillDgvPresence("DESC");
                IncludeDgvPersonalPresenceDeleteLinks();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------

        private string GetGender(int athleteID)
        {
            try
            {
                string str_getAthleteGender = "SELECT CASE WHEN [ismale] = 1 THEN N'آقا' " +
                                                          "WHEN [ismale] = 0 THEN N'خانم' " +
                                                          "ELSE N'' " +
                                                     "END AS [itemIsMaleValue] " +
                                              "FROM [tblAthlete] " +
                                              "WHERE [athlete_ID] = @athlete_ID";

                SqlCommand cmd_getAthleteGender = new SqlCommand(str_getAthleteGender, conn);
                cmd_getAthleteGender.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                SqlDataAdapter adp_getAthleteGender = new SqlDataAdapter(cmd_getAthleteGender);
                DataSet ds_getAthleteGender = new DataSet();
                adp_getAthleteGender.Fill(ds_getAthleteGender);

                if (ds_getAthleteGender.Tables[0].Rows.Count > 0)
                    return ds_getAthleteGender.Tables[0].Rows[0][0].ToString();
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

        private void FillDgvPresence(string sortType)
        {
            string str_getAthletePresence = "SELECT [ساعت ورود] ,[سانس] ,[تعداد جلسه] " +
                                            "FROM view_athletePresence " +
                                            "WHERE [کد] = @athlete_ID AND " +
                                                  "DATEADD(Month, 3, [ساعت ورود]) > GETDATE() " +
                                            "ORDER BY [ساعت ورود] " + sortType;

            SqlCommand cmd_getAthletePresence = new SqlCommand(str_getAthletePresence, conn);
            cmd_getAthletePresence.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = Convert.ToInt32(lblPersonalID.Text);

            SqlDataAdapter adp_getAthletePresence = new SqlDataAdapter(cmd_getAthletePresence);
            DataSet ds_getAthletePresence = new DataSet();
            adp_getAthletePresence.Fill(ds_getAthletePresence);

            if (ds_getAthletePresence.Tables[0].Rows.Count > 0)
                foreach (DataRow row in ds_getAthletePresence.Tables[0].Rows)
                    row["ساعت ورود"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["ساعت ورود"]));

            dgvPersonalPresence.DataSource = ds_getAthletePresence.Tables[0];
            dgvPersonalPresence.Columns["ساعت ورود"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void dgvPersonalPresence_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == presenceFirstDateOrderTemp)
            {
                if (presenceFirstDateOrder)
                {
                    FillDgvPresence("ASC");

                    presenceFirstDateOrder = false;
                    presenceFirstDateOrderTemp = 1;
                }
                else
                {
                    FillDgvPresence("DESC");

                    presenceFirstDateOrder = true;
                }
            }
        }

        private void FillDgvPayment(string sortType)
        {
            string str_getAthleteCharge = "SELECT [payType_title] AS [بخش], [athlete_pay] AS [مبلغ پرداختی], CONVERT(VARCHAR, [charge_date]) AS [تاریخ] " +
                                          "FROM [tblAthleteCharge] INNER JOIN " +
                                          "[tblPayType] ON [tblAthleteCharge].[payType_ID] = [tblPayType].[payType_ID] " +
                                          "WHERE [athlete_ID] = @athlete_ID AND " +
                                                "[athlete_pay] >= 0 AND " +
                                                "DATEADD(YEAR, 1, [charge_date]) > GETDATE()" +
                                          "ORDER BY [تاریخ] " + sortType;

            SqlCommand cmd_getAthleteCharge = new SqlCommand(str_getAthleteCharge, conn);
            cmd_getAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = Convert.ToInt32(lblPersonalID.Text);

            SqlDataAdapter adp_getAthleteCharge = new SqlDataAdapter(cmd_getAthleteCharge);
            DataSet ds_getAthleteCharge = new DataSet();
            adp_getAthleteCharge.Fill(ds_getAthleteCharge);

            if (ds_getAthleteCharge.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds_getAthleteCharge.Tables[0].Rows)
                {
                    row["تاریخ"] = ConvertDateTime.m2sh(Convert.ToDateTime(row["تاریخ"]));

                    if ((int)row["مبلغ پرداختی"] <= 0)
                        row.Delete();
                }
            }

            dgvPersonalPayment.DataSource = ds_getAthleteCharge.Tables[0];
            dgvPersonalPayment.Columns["تاریخ"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void LoadExamData()
        {
            try
            {
                string str_getRecentExam = "SELECT TOP(1) [exam_ID], [exam_serial], [exam_date], [english_name] " +
                                           "FROM [tblAthleteExam] " +
                                           "WHERE [athlete_ID] = @athlete_ID AND " +
                                                 "[isaccepted] = 1 " +
                                           "ORDER BY [exam_date] DESC";

                SqlCommand cmd_getRecentExam = new SqlCommand(str_getRecentExam, conn);
                cmd_getRecentExam.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = Convert.ToInt32(lblPersonalID.Text);

                SqlDataAdapter adp_getRecentExam = new SqlDataAdapter(cmd_getRecentExam);
                DataTable dt_getRecentExam = new DataTable();
                adp_getRecentExam.Fill(dt_getRecentExam);

                if (dt_getRecentExam.Rows.Count > 0)
                {
                    string str_getRecentRankID = "SELECT [rank_ID] " +
                                                 "FROM [tblExam] " +
                                                 "WHERE [exam_ID] = @exam_ID";

                    SqlCommand cmd_getRecentRankID = new SqlCommand(str_getRecentRankID, conn);
                    cmd_getRecentRankID.Parameters.Add("@exam_ID", SqlDbType.Int).Value = dt_getRecentExam.Rows[0].ItemArray[0];

                    SqlDataAdapter adp_getRecentRankID = new SqlDataAdapter(cmd_getRecentRankID);
                    DataTable dt_getRecentRankID = new DataTable();
                    adp_getRecentRankID.Fill(dt_getRecentRankID);

                    string str_getRecentRankName = "SELECT [rank_name] " +
                                                   "FROM [tblRank] " +
                                                   "WHERE [rank_ID] = @rank_ID";

                    SqlCommand cmd_getRecentRankName = new SqlCommand(str_getRecentRankName, conn);
                    cmd_getRecentRankName.Parameters.Add("@rank_ID", SqlDbType.TinyInt).Value = dt_getRecentRankID.Rows[0].ItemArray[0];

                    SqlDataAdapter adp_getRecentRankName = new SqlDataAdapter(cmd_getRecentRankName);
                    DataTable dt_getRecentRankName = new DataTable();
                    adp_getRecentRankName.Fill(dt_getRecentRankName);

                    rankID = Convert.ToInt32(dt_getRecentRankID.Rows[0].ItemArray[0]);
                    examDate = Convert.ToDateTime(dt_getRecentExam.Rows[0].ItemArray[2]);
                    englishName = dt_getRecentExam.Rows[0].ItemArray[3].ToString();

                    txtPersonalExamSerial.Text = dt_getRecentExam.Rows[0].ItemArray[1].ToString();
                    mtxtPersonalExamDate.Text = ConvertDateTime.m2sh(examDate).Substring(0, 10);
                    txtPersonalExamRank.Text = dt_getRecentRankName.Rows[0].ItemArray[0].ToString();

                    txtPersonalExamSerial.Enabled = txtPersonalExamRank.Enabled = mtxtPersonalExamDate.Enabled = btnPersonalExamPrint.Enabled = true;
                }
                else
                {
                    rankID = -1;
                    examDate = default(DateTime);
                    englishName = null;

                    txtPersonalExamSerial.Enabled = txtPersonalExamRank.Enabled = mtxtPersonalExamDate.Enabled = btnPersonalExamPrint.Enabled = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void IncludeDgvPersonalPaymentDeleteLinks()
        {
            try
            {
                DataGridViewLinkColumn deleteLink = new DataGridViewLinkColumn
                {
                    Name = "rowDeleteLink",
                    HeaderText = "",
                    Text = "حذف",
                    TrackVisitedState = false,
                    UseColumnTextForLinkValue = true
                };
                dgvPersonalPayment.Columns.Add(deleteLink);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void IncludeDgvPersonalPresenceDeleteLinks()
        {
            try
            {
                DataGridViewLinkColumn deleteLink = new DataGridViewLinkColumn
                {
                    Name = "rowDeleteLink",
                    HeaderText = "",
                    Text = "حذف",
                    TrackVisitedState = false,
                    UseColumnTextForLinkValue = true
                };
                dgvPersonalPresence.Columns.Add(deleteLink);
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Main Codes --------------------

        private void txtPersonalExamSerial_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtPersonalExamDate_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtPersonalExamRank_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtPersonalPresenceDate_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtPersonalPresenceTime_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void nudPersonalPresenceSessionNum_Enter(object sender, EventArgs e)
        {
            try
            {
                FMainForm.SetLanguage("fa");
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void txtPersonalExamSerial_KeyDown(object sender, KeyEventArgs e)
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

        private void mtxtPersonalPresenceTime_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtPersonalPresenceTime.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtPersonalPresenceTime.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void mtxtPersonalPresenceDate_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (mtxtPersonalPresenceDate.MaskedTextProvider.LastAssignedPosition == -1)
                    mtxtPersonalPresenceDate.SelectionStart = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnPersonalExamPrint_Click(object sender, EventArgs e)
        {
            try
            {
                if (rankID != -1)
                {
                    if (rankID >= 8 && rankID <= 14)
                        FMainForm.PrintSpecialRanks(examDate, txtPersonalExamSerial.Text, englishName, lblPersonalName.Text, GetGender(Convert.ToInt32(lblPersonalID.Text)));
                    else
                        FMainForm.PrintNormalRanks(examDate, txtPersonalExamSerial.Text, englishName, lblPersonalName.Text, GetGender(Convert.ToInt32(lblPersonalID.Text)));
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvPersonalPayment_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvPersonalPayment.RowCount > 0)
                {
                    if (dgvPersonalPayment.CurrentCell.OwningColumn.Name.Equals("rowDeleteLink"))
                    {
                        string chargeDate_Sh = dgvPersonalPayment["تاریخ", e.RowIndex].Value.ToString();
                        int hours = Convert.ToInt32(chargeDate_Sh.Substring(11, 2));
                        int minutes = Convert.ToInt32(chargeDate_Sh.Substring(14, 2));
                        int seconds = Convert.ToInt32(chargeDate_Sh.Substring(17, 2));

                        DateTime chargeDate_M = ConvertDateTime.sh2m(chargeDate_Sh.Substring(0, 2) + " / " + chargeDate_Sh.Substring(3, 2) + " / " + chargeDate_Sh.Substring(6, 4)).Date + new TimeSpan(hours, minutes, seconds);

                        DialogResult result = MessageBox.Show("آیا از حذف تراکنش انتخاب شده اطمینان دارید ؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        if (result == DialogResult.Yes)
                        {
                            string str_getPayTypeID = "SELECT [payType_ID] " +
                                                      "FROM [tblPayType] " +
                                                      "WHERE [payType_title] = @payType_title";

                            SqlCommand cmd_getPayTypeID = new SqlCommand(str_getPayTypeID, conn);
                            cmd_getPayTypeID.Parameters.Add("@payType_title", SqlDbType.NVarChar).Value = dgvPersonalPayment["بخش", e.RowIndex].Value.ToString();

                            SqlDataAdapter adp_getPayTypeID = new SqlDataAdapter(cmd_getPayTypeID);
                            DataTable dt_getPayTypeID = new DataTable();
                            adp_getPayTypeID.Fill(dt_getPayTypeID);

                            string str_deleteAthleteCharge = "DELETE FROM [tblAthleteCharge] " +
                                                             "WHERE [athlete_ID] = @athlete_ID AND " +
                                                                   "[payType_ID] = @payType_ID AND " +
                                                                   "[athlete_pay] = @athlete_pay AND " +
                                                                   "[charge_date] = @charge_date";

                            SqlCommand cmd_deleteAthleteCharge = new SqlCommand(str_deleteAthleteCharge, conn);
                            cmd_deleteAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = Convert.ToInt32(lblPersonalID.Text);
                            cmd_deleteAthleteCharge.Parameters.Add("@payType_ID", SqlDbType.Int).Value = (int)dt_getPayTypeID.Rows[0][0];
                            cmd_deleteAthleteCharge.Parameters.Add("@athlete_pay", SqlDbType.Int).Value = (int)dgvPersonalPayment["مبلغ پرداختی", e.RowIndex].Value;
                            cmd_deleteAthleteCharge.Parameters.Add("@charge_date", SqlDbType.DateTime2).Value = chargeDate_M;

                            conn.Open();
                            cmd_deleteAthleteCharge.ExecuteNonQuery();
                            conn.Close();

                            dgvPersonalPayment.Rows.RemoveAt(e.RowIndex);

                            if (dgvPersonalPayment.Rows.Count > 0)
                                dgvPersonalPayment.CurrentCell = dgvPersonalPayment["بخش", 0];
                            else
                                dgvPersonalPayment.CurrentCell = null;

                            FMainForm.SetSideAthleteDetail(lblPersonalID.Text, pcbPersonalImage, lblPersonalID, lblPersonalName, lblPersonalNumber, lblPersonalRank, lblPersonalCredit);
                            FMainForm.SetSideAthleteDetail(lblPersonalID.Text, pcbPersonalImage, lblPersonalID, lblPersonalName, lblPersonalNumber, lblPersonalRank, lblReportsAthleteCredit);

                            MessageBox.Show("تراکنش مورد نظر با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvPersonalPresence_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dgvPersonalPresence.RowCount > 0)
                {
                    if (dgvPersonalPresence.CurrentCell.OwningColumn.Name.Equals("rowDeleteLink"))
                    {
                        DialogResult result = MessageBox.Show("آیا از حذف حضور انتخاب شده اطمینان دارید ؟", "تایید", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        if (result == DialogResult.Yes)
                        {
                            int athleteID = Convert.ToInt32(lblPersonalID.Text);

                            string presenceDate_Sh = dgvPersonalPresence["ساعت ورود", e.RowIndex].Value.ToString();
                            int hours = Convert.ToInt32(presenceDate_Sh.Substring(11, 2));
                            int minutes = Convert.ToInt32(presenceDate_Sh.Substring(14, 2));
                            int seconds = Convert.ToInt32(presenceDate_Sh.Substring(17, 2));

                            DateTime presenceDate_M = ConvertDateTime.sh2m(presenceDate_Sh.Substring(0, 2) + " / " + presenceDate_Sh.Substring(3, 2) + " / " + presenceDate_Sh.Substring(6, 4)).Date + new TimeSpan(hours, minutes, seconds);

                            string str_deleteAthletePresence = "DELETE FROM [tblAthletePresence] " +
                                                               "WHERE [athlete_ID] = @athlete_ID AND " +
                                                                     "[presence_date] = @presence_date AND " +
                                                                     "[sections_num] = @sections_num";

                            SqlCommand cmd_deleteAthletePresence = new SqlCommand(str_deleteAthletePresence, conn);
                            cmd_deleteAthletePresence.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;
                            cmd_deleteAthletePresence.Parameters.Add("@presence_date", SqlDbType.DateTime2).Value = presenceDate_M;
                            cmd_deleteAthletePresence.Parameters.Add("@sections_num", SqlDbType.TinyInt).Value = Convert.ToByte(dgvPersonalPresence["تعداد جلسه", e.RowIndex].Value);

                            conn.Open();
                            cmd_deleteAthletePresence.ExecuteNonQuery();

                            string str_deleteAthleteCharge = "DELETE FROM [tblAthleteCharge] " +
                                                             "WHERE [athlete_ID] = @athlete_ID AND " +
                                                                   "[payType_ID] = 1 AND " +
                                                                   "[charge_date] = @charge_date";

                            SqlCommand cmd_deleteAthleteCharge = new SqlCommand(str_deleteAthleteCharge, conn);
                            cmd_deleteAthleteCharge.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;
                            cmd_deleteAthleteCharge.Parameters.Add("@charge_date", SqlDbType.DateTime2).Value = presenceDate_M;

                            cmd_deleteAthleteCharge.ExecuteNonQuery();

                            string str_getAthleteActivenessState = "SELECT CASE WHEN DATEADD(MONTH, 3, (SELECT TOP(1) [presence_date] " +
                                                                                                       "FROM [tblAthletePresence] " +
                                                                                                       "WHERE [athlete_ID] = @athlete_ID " +
                                                                                                       "ORDER BY [presence_date] DESC)) < GETDATE() THEN 0 ELSE 1 END";

                            SqlCommand cmd_getAthleteActivenessState = new SqlCommand(str_getAthleteActivenessState, conn);
                            cmd_getAthleteActivenessState.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                            SqlDataAdapter adp_getAthleteActivenessState = new SqlDataAdapter(cmd_getAthleteActivenessState);
                            DataTable dt_getAthleteActivenessState = new DataTable();
                            adp_getAthleteActivenessState.Fill(dt_getAthleteActivenessState);

                            if ((int)dt_getAthleteActivenessState.Rows[0][0] == 0)
                            {
                                string str_updateAthletesActivenessState = "UPDATE [tblAthlete] " +
                                                                           "SET [isactive] = 0 " +
                                                                           "WHERE [athlete_ID] = @athlete_ID";

                                SqlCommand cmd_updateAthletesActivenessState = new SqlCommand(str_updateAthletesActivenessState, conn);
                                cmd_updateAthletesActivenessState.Parameters.Add("@athlete_ID", SqlDbType.Int).Value = athleteID;

                                cmd_updateAthletesActivenessState.ExecuteNonQuery();
                            }

                            conn.Close();

                            dgvPersonalPresence.Rows.RemoveAt(e.RowIndex);

                            if (dgvPersonalPresence.Rows.Count > 0)
                                dgvPersonalPresence.CurrentCell = dgvPersonalPresence["ساعت ورود", 0];
                            else
                                dgvPersonalPresence.CurrentCell = null;

                            FMainForm.InitializeMainSideAthletePresence(dgvMainSideAthletePresence, moneyPerSection);
                            FMainForm.SetSideAthleteDetail(lblPersonalID.Text, pcbPersonalImage, lblPersonalID, lblPersonalName, lblPersonalNumber, lblPersonalRank, lblPersonalCredit);

                            MessageBox.Show("حضور مورد نظر با موفقیت حذف شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnPersonalPresenceAdd_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime date;
                int hours, minutes, seconds;

                if (mtxtPersonalPresenceDate.Text == "   /    / ")
                    MessageBox.Show("تاریخ حضور را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if ((date = ConvertDateTime.sh2m(mtxtPersonalPresenceDate.Text)) == default(DateTime))
                    MessageBox.Show("تاریخ حضور نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (DateTime.Compare(date, DateTime.Now) > 0)
                    MessageBox.Show("تاریخ حضور از تاریخ امروز گذشته است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (mtxtPersonalPresenceTime.Text == "  :  :")
                    MessageBox.Show("زمان حضور وارد نشده است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if (mtxtPersonalPresenceTime.Text.Length < 8)
                    MessageBox.Show("زمان وارد شده نا معتبر است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else if ((hours = Convert.ToInt32(mtxtPersonalPresenceTime.Text.Substring(0, 2))) > 23 ||
                          (minutes = Convert.ToInt32(mtxtPersonalPresenceTime.Text.Substring(3, 2))) > 59 ||
                          (seconds = Convert.ToInt32(mtxtPersonalPresenceTime.Text.Substring(6, 2))) > 59)
                    MessageBox.Show("زمان وارد شده اشتباه است", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                else
                {
                    if (sdk.PresenceTransaction(Convert.ToInt32(lblPersonalID.Text), (int)nudPersonalPresenceSessionNum.Value, date.Date + new TimeSpan(hours, minutes, seconds)) == 1)
                    {
                        System.Threading.Thread.Sleep(1000);

                        mtxtPersonalPresenceDate.ResetText();
                        mtxtPersonalPresenceTime.ResetText();
                        nudPersonalPresenceSessionNum.Value = nudPersonalPresenceSessionNum.Minimum;

                        FillDgvPresence("DESC");
                        FillDgvPayment("DESC");

                        MessageBox.Show("حضور با موفقیت ثبت شد", "نتیجه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("انجام عملیات ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void dgvPersonalPayment_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == paymentFirstDateOrderTemp)
            {
                if (paymentFirstDateOrder)
                {
                    FillDgvPayment("ASC");

                    paymentFirstDateOrder = false;
                    paymentFirstDateOrderTemp = 3;
                }
                else
                {
                    FillDgvPayment("DESC");

                    paymentFirstDateOrder = true;
                }
            }
        }
    }
}
