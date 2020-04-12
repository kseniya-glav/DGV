using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using System.Text.RegularExpressions;

namespace Lab_up_6
{
    
    public partial class dgvFrom : Form
    {
        DataTable table = new DataTable();
        
        DataGridViewCellCancelEventArgs fakThesystem;
        object sender1;
        private readonly string _sConnStr = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = "normalization",
            Username = "postgres",
            Password = "12345",
            MaxAutoPrepare = 10,
            AutoPrepareMinUsages = 2
        }.ConnectionString;

        public dgvFrom()
        {
            InitializeComponent();
            InitializeDgvCups();
            InitializeDgvTrack();
        }

        private void InitializeDgvCups()
        {
            dgvCups.Rows.Clear();
            dgvCups.Columns.Clear();
            dgvCups.Columns.Add( "number_auto","номер авто");
            dgvCups.Columns.Add(new CalendarColumn { DataPropertyName="time_of_issue",Name= "дата выдачи", Visible=true});
            dgvCups.Columns.Add( "number_password","паспорт");
            dgvCups.Columns.Add(new CalendarColumn {  DataPropertyName = "time_delivery", Name= "дата сдачи",Visible=true });
            dgvCups.Columns.Add( "total","сумма");
            

            using (var sConn = new NpgsqlConnection(_sConnStr))
            {
                sConn.Open();

                var sCommand = new NpgsqlCommand
                {
                    Connection = sConn,
                    CommandText = @"select * from receipt order by time_of_issue "
                };
                var reader = sCommand.ExecuteReader();
                while (reader.Read())
                {
                    var dataDict = new Dictionary<string, object>();
                    foreach (var columnsName in new[] { "number_auto", "time_of_issue", "number_password", "time_delivery","total" })
                    {
                        dataDict[columnsName] = reader[columnsName];
                    }
                    var rowIdx = dgvCups.Rows.Add( reader["number_auto"], reader["time_of_issue"], reader["number_password"], reader["time_delivery"], reader["total"]);//, reader["id_zavoda"]);
                    dgvCups.Rows[rowIdx].Tag = dataDict;
                }
            }
        }// норм

       

        private void InitializeDgvTrack()
        {
            dgvTrack.Rows.Clear();
            dgvTrack.Columns.Clear();

            var adresok = new DataGridViewTextBoxColumn
            {
                Name = "address_renter",
                HeaderText = "Город"
            };

            var aidi = new DataGridViewTextBoxColumn
            {
                Name = "number_password",
                HeaderText = "паспорт"
            };
            
            var fullname = new DataGridViewTextBoxColumn
            {
                Name = "fullname_renter",
                HeaderText = "ФИО"
            };
           
            var age = new NumericUpDownColumn
            {
                Name = "age",
                HeaderText = "возраст"
            };


            dgvTrack.Columns.AddRange(aidi,  fullname, adresok, age);
            using (var sConn = new NpgsqlConnection(_sConnStr))
            {
                sConn.Open();

                using (var sCommand = new NpgsqlCommand())
                {
                    sCommand.CommandText = "SELECT number_password,fullname_renter,address_renter,age,numb  FROM renter   order by numb ";//, 
                    sCommand.Connection = sConn;
                    var reader = sCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        dgvTrack.Rows.Add(reader["number_password"], reader["fullname_renter"], reader["address_renter"], reader["age"], reader["numb"]);
                    }
                }
            }


        }// норм

        // возникает при проверке допустимости строки арендаторов
        private void dgvCups_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            var row = dgvCups.Rows[e.RowIndex];
            if (dgvCups.IsCurrentRowDirty)
            {
                
                var cellsWithPotentialErrors = new[] { row.Cells["number_auto"], row.Cells["дата выдачи"], row.Cells["number_password"] , row.Cells["дата сдачи"] , row.Cells["total"] };
                foreach (var cell in cellsWithPotentialErrors)
                {
                    if (cell.Value == null || string.IsNullOrWhiteSpace(cell.Value.ToString()))
                    {
                        row.ErrorText = string.Format("Значение в столбце '{0}' не может быть пустым",
                            cell.OwningColumn.HeaderText);
                        e.Cancel = true;
                    }

                }
                bool a = true, s = true, d = true,f = true,g=true;
                if (!e.Cancel)
                {

                    using (var sConn = new NpgsqlConnection(_sConnStr))
                    {
                        sConn.Open();
                        var sCommand = new NpgsqlCommand
                        {
                            Connection = sConn
                        };

                        string p = row.Cells["number_auto"].Value.ToString().Trim();
                        Regex regex = new Regex(@"[АВЕКМНОРСТУХ]{1}\d{3}[АВЕКМНОРСТУХ]{2}");
                        if (regex.IsMatch(p))
                        {
                            sCommand.Parameters.AddWithValue("@CupNumbAuto", row.Cells["number_auto"].Value.ToString().Trim());
                        }
                        else
                        {
                            row.ErrorText = string.Format("Значение в столбце 'номер авто ' неверно");
                            e.Cancel = true;
                            a = false;
                        }
                        //s = DateTime.Parse(s).ToShortDateString();
                        if (DateTime.TryParse(row.Cells["дата выдачи"].Value.ToString().Trim(),out DateTime p1))
                        {
                            sCommand.Parameters.AddWithValue("@CupTimeOfIssue", p1.Date);
                            sCommand.CommandText = @"select COUNT(*) FROM receipt where number_auto=@CupNumbAuto AND  time_of_issue<@CupTimeOfIssue AND time_delivery>@CupTimeOfIssue";
                            if (Int32.Parse(sCommand.ExecuteScalar().ToString().Trim()) != 0)
                            {
                                row.ErrorText = string.Format("В указанное время прокат автомобиля уже зарегестрирован");
                                e.Cancel = true;
                                s = false;
                            }
                        }
                        else
                        { 
                            row.ErrorText = string.Format("Значение в столбце 'время выдачи чека ' могут принимать только дату не позднее сегодняшней");
                            e.Cancel = true;
                            s = false;
                        }

                        string p2 = row.Cells["number_password"].Value.ToString().Trim();
                        Regex regex2 = new Regex(@"\d{10}");
                        if (regex2.Matches(p2)[0].ToString()==p2)
                        {
                            sCommand.Parameters.AddWithValue("@CupPassword", p2);
                            sCommand.CommandText = @"select COUNT(*) FROM renter WHERE number_password=@CupPassword";
                            int t = Int32.Parse(sCommand.ExecuteScalar().ToString().Trim());
                            if (Int32.Parse(sCommand.ExecuteScalar().ToString().Trim())==0)
                            {
                                row.ErrorText = string.Format("Неверные паспортные данные");
                                e.Cancel = true;
                                d = false;
                            }


                        }
                        else
                        {
                            row.ErrorText = string.Format("Значения в столбце 'пасспорт ' могут состоять только из целочисленных значений");
                            e.Cancel = true;
                            d = false;
                        }

                        if (DateTime.TryParse(row.Cells["дата сдачи"].Value.ToString().Trim(), out DateTime p4))
                        {
                            if (p4 <p1)
                            {
                                row.ErrorText = string.Format("Дата сдачи должна быть позднее даты выдачи");
                                e.Cancel = true;
                                f = false;
                            }
                            else sCommand.Parameters.AddWithValue("@CupTimeDelivery", p4.Date);
                        }
                        else
                        {
                            row.ErrorText = string.Format("Значение в столбце 'дата сдачи ' может содержать только дату");
                            e.Cancel = true;
                            f = false;
                        }

                        if (Int32.TryParse(row.Cells["total"].Value.ToString().Trim(), out int p3))
                        {
                            if (p3 < 0)
                            {
                                row.ErrorText = string.Format("Значение в столбце 'сумма ' могут принимать только целочисленные значения больше 0");
                                e.Cancel = true;
                                 g= false;
                            }
                            else
                            {
                                sCommand.Parameters.AddWithValue("@CupTotal", p3);

                            }

                        }
                        else
                        {

                            row.ErrorText = string.Format("Значение в столбце 'сумма ' могут принимать только целочисленные значения");
                            e.Cancel = true;
                            g = false;

                        }
                        
                        if (a&&s&&d&&f&&g)
                        {
                              //первичные ключи
                              sCommand.CommandText = @"select COUNT(*) FROM receipt WHERE number_auto = @CupNumbAuto And
                                                          time_of_issue = @CupTimeOfIssue";
                              if (Int32.Parse(sCommand.ExecuteScalar().ToString().Trim())>0) //проверка на повторяющиеся строки
                              {
                                sCommand.CommandText = @"select COUNT(*) FROM receipt WHERE number_auto = @CupNumbAuto And
                                                          time_of_issue = @CupTimeOfIssue  And number_password = @CupPassword
                                                                   And time_delivery=@CupTimeDelivery And total=@CupTotal";
                                if (Int32.Parse(sCommand.ExecuteScalar().ToString().Trim()) == 0)
                                {
                                    sCommand.CommandText = @"UPDATE receipt SET number_auto = @CupNumbAuto,
                                                          time_of_issue = @CupTimeOfIssue, number_password = @CupPassword,time_delivery=@CupTimeDelivery, total=@CupTotal
                                                        WHERE number_auto = @CupNumbAuto And time_of_issue=@CupTimeOfIssue";
                                    sCommand.ExecuteNonQuery();
                                }
                                else
                                {
                                    f = false;
                                    row.ErrorText = string.Format("Строка с такими  данными уже существует");
                                    e.Cancel = true;
                                }
                              }
                              else
                              {
                                    
                                    //сохранение новой строки 
                                    sCommand.CommandText = @"insert into receipt(number_auto,time_of_issue,number_password,time_delivery,total)
                                                        values (@CupNumbAuto,@CupTimeOfIssue,@CupPassword,@CupTimeDelivery,@CupTotal);";
                                    row.Cells["number_auto"].Value = sCommand.ExecuteScalar();

                                    sCommand.CommandText = @" select number_auto from receipt where number_auto = @CupNumbAuto";
                                    row.Cells["number_auto"].Value = sCommand.ExecuteScalar();
                              }
                                
                                var dataDict = new Dictionary<string, object>();
                                foreach (var columnsName in new[] { "number_auto", "дата выдачи", "number_password", "дата сдачи","total" })
                                {
                                    dataDict[columnsName] = row.Cells[columnsName].Value;
                                }
                                row.Tag = dataDict;
                            
                        }
                    }
                    if (a && s && d&&f&&g)
                    {
                        row.ErrorText = "";
                        foreach (var cell in cellsWithPotentialErrors)
                        {
                            cell.ErrorText = "";
                        }
                    }
                        
                }
            }
            sender1 = sender;
            fakThesystem = e;
        }

        // изменение чека 
        private void dgvCups_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!dgvCups.Rows[e.RowIndex].IsNewRow)
            {
                dgvCups[e.ColumnIndex, e.RowIndex].ErrorText = "Значение изменено и пока не сохранено.";
            }
        }//норм наверное
        // при удалении строки чека
        private void dgvCups_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var CupNumbAuto = e.Row.Cells["number_auto"].Value.ToString().Trim();
            DateTime CupTimeOfIssue = DateTime.Parse(e.Row.Cells["дата выдачи"].Value.ToString().Trim());

            using (var sConn = new NpgsqlConnection(_sConnStr))
                {
                    sConn.Open();
                    var sCommand = new NpgsqlCommand
                    {
                        Connection = sConn,
                        CommandText = "DELETE FROM receipt WHERE number_auto = @CupNumbAuto And time_of_issue=@CupTimeOfIssue"
                    };
                    sCommand.Parameters.AddWithValue("@CupNumbAuto", CupNumbAuto);
                    sCommand.Parameters.AddWithValue("@CupTimeOfIssue", CupTimeOfIssue.Date);

                    sCommand.ExecuteNonQuery();
            }
            
        }//норм

        // возникает при проверке допустимости строки арендатора
        private void dgvTrack_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            var row = dgvTrack.Rows[e.RowIndex];
            if (dgvTrack.IsCurrentRowDirty)
            {
                var cellsWithPotentialErrors = new[] { row.Cells["number_password"], row.Cells["fullname_renter"], row.Cells["address_renter"], row.Cells["age"] };
                foreach (var cell in cellsWithPotentialErrors)
                {
                    if (cell.Value == null || string.IsNullOrWhiteSpace(cell.Value.ToString().Trim()))
                    {
                        row.ErrorText = string.Format("Значение в столбце '{0}' не может быть пустым",
                            cell.OwningColumn.HeaderText);
                        e.Cancel = true;
                    }
                }
                bool a = true, b = true, c = true, d = true, updatePassword = true ;

                if (!e.Cancel)
                {

                    using (var sConn = new NpgsqlConnection(_sConnStr))
                    {
                        sConn.Open();
                        var sCommand = new NpgsqlCommand
                        {
                            Connection = sConn
                        };
                                            
                        string p3 = row.Cells["fullname_renter"].Value.ToString().Trim();
                        Regex regex3 = new Regex(@"[А-ЯЁ][а-яё]*\s{1}[А-ЯЁ](.)[А-ЯЁ](.)");
                        if (regex3.Matches(p3)[0].ToString()==p3)
                        {
                            sCommand.Parameters.AddWithValue("@TrackFIO", p3);
                        }
                        else
                        {
                            row.ErrorText = string.Format("Значения в столбце 'ФИО ' не соответствуют единому стилю");
                            e.Cancel = true;
                            b = false;
                        }
                            
                        string p4 = row.Cells["address_renter"].Value.ToString().Trim();
                        Regex regex4 = new Regex(@"[А-ЯЁ][а-яё]*");
                        if (regex4.Matches(p4)[0].ToString()==p4)
                        {
                            sCommand.Parameters.AddWithValue("@TrackAdress", p4);
                        }
                        else
                        {
                            row.ErrorText = string.Format("Значения в столбце 'Город' могут состоять только из символов алфавита ");
                            e.Cancel = true;
                            c = false;
                        }

                        string p1 = row.Cells["age"].Value.ToString().Trim();
                        int pp1 = Int32.Parse(p1);
                        if (pp1 >= 18 && pp1<=100)
                        {
                            sCommand.Parameters.AddWithValue("@TrackAge", pp1);
                            updatePassword = true;
                        }
                        else
                        {
                            row.ErrorText = string.Format("Арендатору должно быть больше 18 лет и меньше 101");
                            e.Cancel = true;
                            d = false;
                        }

                        string p2 = row.Cells["number_password"].Value.ToString().Trim();
                        Regex regex2 = new Regex(@"\d{10}");
                        if (regex2.Matches(p2)[0].ToString()==p2)
                        {
                            sCommand.Parameters.AddWithValue("@TrackPassword", p2);
                            sCommand.CommandText = @"select COUNT(*) FROM renter WHERE number_password = @TrackPassword";
                            if (Int32.Parse(sCommand.ExecuteScalar().ToString().Trim()) == 1) //если запись с таким паспортом уже есть
                            {
                                //номер выделенной строки
                                int numStr = dgvTrack.CurrentRow.Index + 1;
                                sCommand.Parameters.AddWithValue("@Numb", numStr);
                                //общее кол-во строк в таблице
                                int kolstr = dgvTrack.Rows.Count;

                                sCommand.CommandText = @"select count(*) FROM renter WHERE number_password = @TrackPassword and numb=@Numb";
                                int num = Int32.Parse(sCommand.ExecuteScalar().ToString().Trim());
                                if (num == 1)
                                {
                                   // sCommand.Parameters.AddWithValue("@TrackPassword", p2);
                                    updatePassword = true;
                                }
                                else if (numStr != kolstr-1 )
                                {
                                    row.ErrorText = string.Format("Значение в столбце 'пасспорт ' изменять нельзя");
                                    e.Cancel = true;
                                    a = false;
                                    updatePassword = false;
                                } else if (numStr == kolstr-1)
                                {
                                    a = false;
                                    row.ErrorText = string.Format("Арендатор с указанным паспортом уже существует в базе");
                                    e.Cancel = true;
                                    updatePassword = false;
                                }
                            } else 
                            {
                                //номер выделенной строки
                                int numStr = dgvTrack.CurrentRow.Index + 1;
                                sCommand.Parameters.AddWithValue("@Numb", numStr);
                                //общее кол-во строк в таблице
                                int kolstr = dgvTrack.Rows.Count;
                                
                                if (numStr != kolstr && numStr!=kolstr-1)
                                {
                                    row.ErrorText = string.Format("Значение в столбце 'пасспорт ' изменять нельзя");
                                    e.Cancel = true;
                                    a = false;
                                    updatePassword = false;
                                }
                            }
                        }
                        else
                        {
                            row.ErrorText = string.Format("Значения в столбце 'пасспорт ' могут состоять только из целочисленных значений");
                            e.Cancel = true;
                            a = false;
                            updatePassword = false;
                        }


                        // sCommand.Parameters.AddWithValue("@TrackVoz", int.Parse((string)row.Cells["vozrast"].Value));


                        if (a&&b&&c&&d)
                        {
                            //номер выделенной строки
                            int numStr = dgvTrack.CurrentRow.Index + 1;
                            //общее кол-во строк в таблице
                            int kolstr = dgvTrack.Rows.Count;
                            if (updatePassword || numStr != kolstr - 1)
                            {
                                sCommand.CommandText = @"UPDATE renter SET number_password=@TrackPassword, fullname_renter=@TrackFIO, address_renter=@TrackAdress, age=@TrackAge WHERE number_password=@TrackPassword";
                               // sCommand.Parameters.AddWithValue("@TrackPassword", p2);
                                sCommand.ExecuteNonQuery();
                            }
                            else
                            {
                                sCommand.CommandText = @"INSERT INTO renter(number_password,fullname_renter,address_renter,age)
                                                      VALUES (@TrackPassword,@TrackFIO, @TrackAdress, @TrackAge)";

                                sCommand.ExecuteNonQuery();

                            }

       
                        }
                     

                    }
                    if (a&&b&&c&&d)
                    {
                        row.ErrorText = "";
                        foreach (var cell in cellsWithPotentialErrors)
                        {
                            cell.ErrorText = "";
                        }
                    }
                }
            }
        }

        // изменение арендатора
        private void dgvTrack_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!dgvTrack.Rows[e.RowIndex].IsNewRow)
            {
                dgvTrack[e.ColumnIndex, e.RowIndex].ErrorText = "Значение изменено и пока не сохранено.";
                
            }
        }//норм наверное
        // при удалении строки арендатора
        private void dgvTrack_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var TrackPassword = e.Row.Cells["number_password"].Value.ToString().Trim();
            using (var sConn = new NpgsqlConnection(_sConnStr))
            {
                    sConn.Open();
                var sCommand = new NpgsqlCommand
                {
                    Connection = sConn,
                    CommandText = @"select count(*) FROM receipt WHERE number_password = @TrackPassword"
                };
                sCommand.Parameters.AddWithValue("@TrackPassword", TrackPassword);
                int znach = Int32.Parse(sCommand.ExecuteScalar().ToString().Trim());
                if (znach > 0)
                {
                    e.Row.ErrorText = string.Format("Нельзя удалить арендатора, у которого есть история проката авто");
                    e.Cancel = true;
                }
                else {
                    sCommand.CommandText = @"DELETE FROM renter WHERE number_password = @TrackPassword";

                    sCommand.Parameters.AddWithValue("@TrackPassword", TrackPassword);
                    sCommand.ExecuteNonQuery();

                    sCommand.CommandText = @"ALTER TABLE renter DROP COLUMN numb";
                    sCommand.ExecuteNonQuery();
                    sCommand.CommandText = @"ALTER TABLE renter ADD numb serial;";
                    sCommand.ExecuteNonQuery();
                }
              

                //sCommand.Parameters.AddWithValue("@TrackPassword", TrackPassword);

                //InitializeDgvCups();
               // InitializeDgvTrack();
                //sCommand.ExecuteNonQuery();
              
            }

        }//норм

        // срабатывает при нажатии клавиши перед выполнением нажатия чека
        private void dgvCups_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && dgvCups.IsCurrentRowDirty)
            {
                dgvCups.CancelEdit();
                if (dgvCups.CurrentRow.Cells["number_auto"].Value != null)
                {
                    try
                    {
                        foreach (var kvp in (Dictionary<string, object>)dgvCups.CurrentRow.Tag)
                        {
                            dgvCups.CurrentRow.Cells[kvp.Key].Value = kvp.Value;
                            dgvCups.CurrentRow.Cells[kvp.Key].ErrorText = "";
                            dgvCups_RowValidating(sender1, fakThesystem);
                        }
                    }
                    catch { }
                    
                }
                else
                {
                    dgvCups.Rows.Remove(dgvCups.CurrentRow);
                }
            }
        }
        // срабатывает при нажатии клавиши перед выполнением нажатия арендатора
        private void dgvTrack_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && dgvTrack.IsCurrentRowDirty)
            {
                dgvTrack.CancelEdit();
                if (dgvTrack.CurrentRow.Cells["number_password"].Value != null)
                {
                    try
                    {
                        foreach (var kvp in (Dictionary<string, object>)dgvTrack.CurrentRow.Tag)
                        {
                            dgvTrack.CurrentRow.Cells[kvp.Key].Value = kvp.Value;
                            dgvTrack.CurrentRow.Cells[kvp.Key].ErrorText = "";
                        }
                    }
                    catch
                    {

                    }
                   
                }
                else
                {
                    dgvTrack.Rows.Remove(dgvTrack.CurrentRow);
                }
            }
        }

        private void dgvTrack_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }
    }
}


