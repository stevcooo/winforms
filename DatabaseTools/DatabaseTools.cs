using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DatabaseTools
{
    public enum DatabaseObject
    {
        Procedure = 0,
        Function = 1,
        View = 2,
        Table = 3
    }

    public partial class DatabaseTools : Form
    {
        private string m_DBConnStr;
        List<RefferencedTable> results;
        public DatabaseTools()
        {
            InitializeComponent();
            InitializeConnectionString();
            InitializeControls();            
            results = new List<RefferencedTable>();
        }


        private void SetConnectionString(String Name)
        {
            m_DBConnStr = ConfigurationManager.ConnectionStrings[Name].ConnectionString;
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder.ConnectionString = m_DBConnStr;
            lblSqlServer.Text = builder.DataSource;
            lblDatabase.Text = builder.InitialCatalog;
        }

        private void InitializeConnectionString()
        {
            SetConnectionString("DefaultDatabaseConnection");
        }

        private List<String> GetDatabaseConnectionStrings()
        {
            List<String> result = new List<String>();            

            foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                if (!connectionString.Name.Equals("LocalSqlServer"))
                    result.Add(connectionString.Name);

            return result;
        }

        private void InitializeControls()
        {
            

            cbDbSchemas.DataSource = GetDatabaseSchemas();
            cbSchemas.DataSource = GetDatabaseSchemas();
            
            if (cbDatabaseSelect.Items.Count == 0)
                cbDatabaseSelect.DataSource = GetDatabaseConnectionStrings();
            
            if(cbDatabaseSelect.SelectedItem == null)
                cbDatabaseSelect.SelectedItem = "DefaultDatabaseConnection";


            tbProceduresToBeScripted.Text = @"[dbo].[sp_GetExistingInsurancePolicy]
[dbo].[sp_PreProcsLoanArrAggr_RBBG]
[dbo].[BC_Basel2GARAAutomatedResult]
[dbo].[Basel2GARAAutomatedAnswers]
[dbo].[sp_CollateralMatchSD]
[dbo].[sp_PutDealGetCollateralTasks]
[dbo].[sp_InsertMarketValue]
[dbo].[sp_GetOverdueReviewRevaluation]
[dbo].[sp_CheckIfObjectExists]
[dbo].[sp_UpdateMarketValueStatus]
[dbo].[CMS_ORTRevaluation]
[dbo].[sp_GetCollateralContractExpiration]
[dbo].[BC_Basel2NotApplicableAutomatedResult]
[dbo].[sp_GreenContractEndDate]
[dbo].[sp_Validate_FSAContractDateValidate]
[dbo].[sp_InsurancePolicyEndDateNotification]
dbo.sp_ProcessImportedPortfolioRevaluation
[dbo].[sp_PrintRecoveryAndRealizationReporting]
[dbo].[sp_PrintRatingDownUpgradesGuarantees]
[dbo].[sp_PrintInsuranceCoverage]
[dbo].[sp_PrintConcentrationMonitoringGuarantee]
[dbo].[sp_PrintExpandedMainValueChanges]
[dbo].[sp_PrintExpiryReport]
[dbo].[sp_PrintMainValueChanges]
[dbo].[sp_PrintOverdueRevaluations]
[dbo].[sp_PrintMinimalCollateralCoverage]
[dbo].[sp_PrintRatingDownUpgradesDebtSecurities]
[dbo].[sp_PrintExpandedMinimalCollateralCoverage]
[dbo].[sp_PrintCESReportDS3]
[dbo].[sp_PrintDueRevaluations]
[dbo].[GetMessageForUniquenessResourceItem]
";
            tbViewsToBeScripted.Text = @"[dbo].[CollateralContractView]
[dbo].[CollateralInsurancePolicyNewView]
[dbo].[CollateralInsurancePolicyView]
[dbo].[CollateralNotesView]
[dbo].[CollateralObjectView]  
[dbo].[CollateralSecuredDealMatchingIsIndependentView]
[dbo].[CollateralTaskWorkItemView]
[dbo].[LU_CollateralContract]
[dbo].[LU_CollateralObjectTypes]
[dbo].[LU_CollateralSerialNumber]
[dbo].[LU_OrganizationSubtypes]
[dbo].[ScreenConfigurationView]
[dbo].[COCCView]
";

        }

        private void GenerateTextForTablesUpdate()
        {
            WriteLine("---- Update tables -----", tbShiftTableResults);
            WriteLine("BEGIN TRAN", tbShiftTableResults);
            foreach (RefferencedTable tmp in results)
            {
                if (cbIncrement.Checked)
                    WriteLine("UPDATE [" + tmp.SchemaName + "].[" + tmp.TableName + "] SET [" + tmp.ColumnName + "] = [" + tmp.ColumnName + "] + " + tbValue.Text + " Where " + tmp.ColumnName + " is not null", tbShiftTableResults);
                else
                    WriteLine("UPDATE [" + tmp.SchemaName + "].[" + tmp.TableName + "] SET [" + tmp.ColumnName + "] = [" + tmp.ColumnName + "] - " + tbValue.Text + " Where " + tmp.ColumnName + " is not null", tbShiftTableResults);
            }
            WriteLine("COMMIT TRAN", tbShiftTableResults);
            WriteLine("---- END Update tables -----", tbShiftTableResults);
        }

        private void GenerateTextForConstraintsDisable()
        {
            WriteLine("---- Disable constraints-----", tbShiftTableResults);
            foreach (RefferencedTable tmp in results)
                WriteLine("ALTER TABLE [" + tmp.SchemaName + "].[" + tmp.TableName + "] NOCHECK CONSTRAINT ALL", tbShiftTableResults);
            WriteLine("---- END Disable constraints-----", tbShiftTableResults);
        }

        private void GenerateTextForConstraintsEnable()
        {
            WriteLine("---- Endable constraints-----", tbShiftTableResults);
            foreach (RefferencedTable tmp in results)
                WriteLine("ALTER TABLE [" + tmp.SchemaName + "].[" + tmp.TableName + "] WITH CHECK CHECK CONSTRAINT ALL", tbShiftTableResults);
            WriteLine("---- END Endable constraints-----", tbShiftTableResults);
        }

        protected void WriteLine(String Line, TextBox tb)
        {
            tb.AppendText(Line);
            tb.AppendText(Environment.NewLine);
        }

        protected void WriteLineIfExists(String Object, TextBox tb, DatabaseObject objectType)
        {
            Object = Object.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            switch (objectType)
            {
                case DatabaseObject.View:
                    tb.AppendText("IF EXISTS (SELECT * FROM SYS.VIEWS WHERE NAME = '" + Object + "')");
                    tb.AppendText(Environment.NewLine);
                    tb.AppendText("     DROP VIEW " + Object);
                    break;
                case DatabaseObject.Procedure:
                    tb.AppendText("IF EXISTS (SELECT * FROM SYS.PROCEDURES WHERE NAME = '" + Object + "')");
                    tb.AppendText(Environment.NewLine);
                    tb.AppendText("     DROP PROCEDURE " + Object);
                    break;
                case DatabaseObject.Function:
                    tb.AppendText("IF EXISTS (SELECT * FROM SYS.FUNCTIONS WHERE NAME = '" + Object + "')");
                    tb.AppendText(Environment.NewLine);
                    tb.AppendText("     DROP FUNCTION " + Object);
                    break;
                default: break;
            }
            tb.AppendText(Environment.NewLine);
            tb.AppendText("GO");
            tb.AppendText(Environment.NewLine);
        }

        private String CheckInput()
        {
            String message = String.Empty;

            if (!(cbDbSchemas.SelectedItem is object))
                message += " You must specify schema!";

            if (!(cbTables.SelectedItem is object))
                message += " You must specify table!";

            if (!(cbColumns.SelectedItem is object))
                message += " You must specify column!";

            Int64 value = -1;
            if (!Int64.TryParse(tbValue.Text, out value))
            {
                message += " You must specify Value as a positive Integer!";
            }

            if (value < 0)
                message += " You must specify Value as a positive Integer!";

            return message;
        }

        private void GetTables(RefferencedTable Table)
        {
            if (!results.Exists(t => t.TableName == Table.TableName && t.ParentTableName == Table.ParentTableName && t.ColumnName == Table.ColumnName && t.IsProcessed == true))
            {
                Table.IsProcessed = true;

                if (!String.IsNullOrEmpty(Table.ParentTableName)) //Da ne ja dodava pocetnata tabela dva pati
                    results.Add(Table);

                List<RefferencedTable> tables = GetRefferencedTables(Table.TableName, Table.ColumnName);

                foreach (RefferencedTable table in tables)
                    if (!results.Exists(t => t.TableName == Table.TableName && t.ParentTableName == Table.ParentTableName && t.ColumnName == Table.ColumnName && t.IsProcessed == false))
                        GetTables(table);
            }
        }



        #region Database operations

        private bool TestDbConnection()
        {
            bool result = true;
            DbConnection conn = new SqlConnection(m_DBConnStr);

            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select count(*) from sys.tables";
            cmd.CommandType = System.Data.CommandType.Text;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                result = false;
                WriteLog("TestDbConnection::", ex);
            }
            finally
            {
                conn.Close();
            }
            return result;
        }

        private string TestSpHelptext()
        {
            DbConnection conn = new SqlConnection(m_DBConnStr);

            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "sp_helptext";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;


            DbParameter par = cmd.CreateParameter();
            par.ParameterName = "@objname";
            par.Value = "Arrangement";
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            DbDataReader rdr = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    //sb.AppendLine((string)rdr["Text"]);
                    sb.Append((string)rdr["Text"]);
                }

            }
            catch (Exception ex)
            {
                WriteLog("GetRefferencedTables::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }

            return sb.ToString();
        }

        private string SpHelptext(String objectName)
        {
            DbConnection conn = new SqlConnection(m_DBConnStr);

            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "sp_helptext";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;


            DbParameter par = cmd.CreateParameter();
            par.ParameterName = "@objname";
            par.Value = objectName.Replace("[","").Replace("]","");
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            DbDataReader rdr = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                    sb.Append((string)rdr["Text"]);

            }
            catch (Exception ex)
            {
                WriteLog("SpHelptext::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }

            return sb.ToString();
        }

        private List<String> GetDatabaseSchemas()
        {
            List<String> result = new List<string>();
            DbConnection conn = new SqlConnection(m_DBConnStr);
            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select DISTINCT(sc.Name) FROM sys.schemas sc JOIN sys.tables t ON sc.schema_id = t.schema_id order by 1";
            cmd.CommandType = System.Data.CommandType.Text;

            DbDataReader rdr = null;
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                    result.Add((string)rdr["Name"]);

            }
            catch (Exception ex)
            {
                WriteLog("GetDatabaseSchemas::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }
            return result;
        }

        private List<String> GetDatabaseTables(String SchemaName)
        {
            List<String> result = new List<string>();
            DbConnection conn = new SqlConnection(m_DBConnStr);
            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select t.NAME FROM  sys.tables t JOIN sys.schemas sc ON t.schema_id = sc.schema_id WHERE sc.name = '" + SchemaName + "' order by 1";
            cmd.CommandType = System.Data.CommandType.Text;

            DbDataReader rdr = null;
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                    result.Add((string)rdr["Name"]);

            }
            catch (Exception ex)
            {
                WriteLog("GetDatabaseTables::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }
            return result;
        }

        private List<String> GetDatabaseColumns(String TableName)
        {
            List<String> result = new List<string>();
            DbConnection conn = new SqlConnection(m_DBConnStr);
            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select c.Name FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = '" + TableName + "' order by 1";
            cmd.CommandType = System.Data.CommandType.Text;

            DbDataReader rdr = null;
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                    result.Add((string)rdr["Name"]);

            }
            catch (Exception ex)
            {
                WriteLog("GetDatabaseColumns::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }
            return result;
        }

        private List<RefferencedTable> GetRefferencedTables(String TableName, String ColumnName)
        {
            List<RefferencedTable> result = new List<RefferencedTable>();
            DbConnection conn = new SqlConnection(m_DBConnStr);

            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetAllReferencedTables";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            DbParameter par = cmd.CreateParameter();
            par.ParameterName = "@TableName";
            par.Value = TableName;
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            par = cmd.CreateParameter();
            par.ParameterName = "@ColumnName";
            par.Value = ColumnName;
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            DbDataReader rdr = null;
            RefferencedTable tmp;
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    tmp = new RefferencedTable();
                    tmp.ParentTableName = (string)rdr["ParentTableName"];
                    tmp.TableName = (string)rdr["TableName"];
                    tmp.ColumnName = (string)rdr["ColName"];
                    tmp.SchemaName = (string)rdr["SchemaName"];
                    result.Add(tmp);
                }

            }
            catch (Exception ex)
            {
                WriteLog("GetRefferencedTables::", ex);                
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }

            return result;
        }

        private String CreateQueryForTableRowsCopy(String Schema, String Table, String SourceDb, String DestinationDb)
        {
            String result = String.Empty;
            DbConnection conn = new SqlConnection(m_DBConnStr);

            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "sp_CreateQueryForTableRowsCopy";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            DbParameter par = cmd.CreateParameter();
            par.ParameterName = "@Table";
            par.Value = Table.Replace("["+Schema+"].", "").Replace("[", "").Replace("]", "");
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            par = cmd.CreateParameter();
            par.ParameterName = "@Schema";
            par.Value = Schema;
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            par = cmd.CreateParameter();
            par.ParameterName = "@SourceDB";
            par.Value = SourceDb;
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            par = cmd.CreateParameter();
            par.ParameterName = "@DestinationDB";
            par.Value = DestinationDb;
            par.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(par);

            DbDataReader rdr = null;
            try
            {
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                    result = ((string)rdr["Result"]);

            }
            catch (Exception ex)
            {
                WriteLog("CreateQueryForTableRowsCopy::", ex);
            }
            finally
            {
                if (rdr != null)
                    rdr.Close();
                conn.Close();
            }

            return result;
        }

        #endregion

        private void WriteLog(String text, Exception e)
        {
            txtLog.AppendText(DateTime.Now.ToLongTimeString() + ": " + text);

            txtLog.AppendText(e.Message);

            if (e.InnerException is object)
                WriteLog(".Inner exception: ", e.InnerException);

            txtLog.AppendText(Environment.NewLine);

            MessageBox.Show("Error occured, please check Log tab.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            results = new List<RefferencedTable>();
            String ValidationResult = CheckInput();

            if (!String.IsNullOrWhiteSpace(ValidationResult))
                MessageBox.Show(ValidationResult, "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                PleaseWaitForm l = new PleaseWaitForm("Please Wait...");
                l.StartPosition = FormStartPosition.CenterParent;
                l.Show();
                l.Refresh();

                RefferencedTable input = new RefferencedTable();
                input.TableName = cbTables.SelectedItem.ToString();
                input.ColumnName = cbColumns.SelectedItem.ToString();
                input.SchemaName = cbDbSchemas.SelectedItem.ToString();
                GetTables(input);

                results.Add(input);

                l.Close();
                l.Dispose();

                GenerateTextForConstraintsDisable();
                GenerateTextForTablesUpdate();
                GenerateTextForConstraintsEnable();
            }
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            bool test = TestDbConnection();
            if (test)
                MessageBox.Show("Connection is OK", "OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Connection is NOT OK", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }

        private void btnClearResults_Click(object sender, EventArgs e)
        {
            tbShiftTableResults.Clear();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveForm("GeneratedScript.sql", tbShiftTableResults.Text);
        }

        private void cbTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbTables.SelectedItem is object)
                cbColumns.DataSource = GetDatabaseColumns(cbTables.SelectedItem.ToString());
        }

        private void cbDbSchemas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDbSchemas.SelectedItem is object)
                cbTables.DataSource = GetDatabaseTables(cbDbSchemas.SelectedItem.ToString());
        }

        private void MainTab_Click(object sender, EventArgs e)
        {

        }

        private void btnTestSpHelpText_Click(object sender, EventArgs e)
        {
            tbTestResults.Text = TestSpHelptext();
        }

        private void btnScriptProcedures_Click(object sender, EventArgs e)
        {
            SpHelptextScript(tbProceduresToBeScripted, tbProceduresToBeScriptedResult, DatabaseObject.Procedure);
        }

        private void tbScriptViews_Click(object sender, EventArgs e)
        {
            SpHelptextScript(tbViewsToBeScripted, tbViewsToBeScriptedResult, DatabaseObject.View);
        }

        private void btnSaveStoredProcedures_Click(object sender, EventArgs e)
        {
            SaveForm("ScriptedStoredProcedures.sql", tbProceduresToBeScriptedResult.Text);
        }

        private void tbSaveViews_Click(object sender, EventArgs e)
        {
            SaveForm("ScriptedViews.sql", tbViewsToBeScriptedResult.Text);
        }

        private void btnClearViews_Click(object sender, EventArgs e)
        {
            tbViewsToBeScriptedResult.Clear();
        }

        private void btnClearProcedures_Click(object sender, EventArgs e)
        {
            tbProceduresToBeScriptedResult.Clear();
        }

        private void tbSaveFunctions_Click(object sender, EventArgs e)
        {
            SaveForm("ScriptedFunctions.sql", tbFunctionsToBeScriptedResult.Text);            
        }

        private void tbGenerateScriptFunctions_Click(object sender, EventArgs e)
        {
            SpHelptextScript(tbFunctionsToBeScripted, tbFunctionsToBeScriptedResult, DatabaseObject.Function);
        }

        private void SpHelptextScript(TextBox SourceStrings, TextBox Results, DatabaseObject ObjectType)
        {
            StringReader reader = new StringReader(SourceStrings.Text);
            String line = String.Empty;

            while (true)
            {
                line = reader.ReadLine();
                if (line != null)
                {
                    if (line.Equals("\n") || line.Equals("\r") || line.Equals("\r") || line.Equals(""))
                        continue;

                    String result = SpHelptext(line.Trim());
                    if (!String.IsNullOrWhiteSpace(result))
                    {
                        WriteLine("-----" + line + "-----", Results);
                        WriteLineIfExists(line, Results, ObjectType);
                        WriteLine(result, Results);                        
                        WriteLine("----- END " + line + "-----", Results);                        
                        WriteLine("GO", Results);
                    }
                    else
                        WriteLine("-----" + line + " DOES NOT EXISTS-----", Results);
                }
                else
                    break;
            }
        }

        private void btnClearFunctions_Click(object sender, EventArgs e)
        {
            tbFunctionsToBeScriptedResult.Clear();
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void DatabaseTools_Load(object sender, EventArgs e)
        {

        }

        private void cbDatabaseSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void btnChangeDB_Click(object sender, EventArgs e)
        {
            cbDatabaseSelect.Enabled = true;
        }

        private void btnSaveDB_Click(object sender, EventArgs e)
        {
            if (cbDatabaseSelect.SelectedItem != null)
                SetConnectionString(cbDatabaseSelect.SelectedItem.ToString());
            
            cbDatabaseSelect.Enabled = false;
            
            InitializeControls();
            results = new List<RefferencedTable>();
        }

        private void btnGenerateCopyTableRows_Click(object sender, EventArgs e)
        {

            String Schema = String.Empty;
            if( cbSchemas.SelectedItem != null)
                Schema = cbSchemas.SelectedItem.ToString();
            
            String SourceDb = String.Empty;
            SourceDb = lblDatabase.Text;
            String DestinationDb = String.Empty;
            DestinationDb  = tbDestinationDB.Text;

            StringReader reader = new StringReader(tbTablesToBeCopied.Text);
            String line = String.Empty;

            while (true)
            {
                line = reader.ReadLine();
                if (line != null)
                {
                    if (line.Equals("\n") || line.Equals("\r") || line.Equals("\r") || line.Equals(""))
                        continue;

                    String result = CreateQueryForTableRowsCopy(Schema, line.Trim(), SourceDb, DestinationDb);
                    if (!String.IsNullOrWhiteSpace(result))
                    {
                        WriteLine("-----" + line + "-----", tbTablesToBeCopiedResults);
                        tbTablesToBeCopiedResults.AppendText(result);
                        WriteLine("----- END " + line + "-----", tbTablesToBeCopiedResults);
                    }
                    else
                        WriteLine("-----" + line + " DOES NOT EXISTS-----", tbTablesToBeCopiedResults);
                }
                else
                    break;
            }
        }

        private void cbSchemas_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tbTablesToBeCopiedResults.Clear();                
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveForm("TableRowsToBeCopied.sql", tbTablesToBeCopiedResults.Text);
        }

        public void SaveForm(String Title, String Text)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = Title;
            savefile.Filter = "SQL (*.sql)|*.sql";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(savefile.FileName))
                    sw.Write(Text);
            }
        }

        private void btnFindGaps_Click(object sender, EventArgs e)
        {

        }
    }
}
