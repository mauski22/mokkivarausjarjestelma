using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mokkivarausjarjestelma
{
    public partial class FormVaraus : Form
    {
        MySqlConnection connection = new MySqlConnection("datasource=localhost;port=3307;database=vn;username=root;password=Ruutti");
        private DateTime vahvistuspvm;
        private bool muokkausMenossa = false;
        private bool haunRajausPaalla = false;
        public FormVaraus()
        {
            InitializeComponent();
            this.Shown += Form_Shown;
            using (connection)
            {
                try
                {
                    // täyttää asiakas_id comboboxin
                    string asiakasnimetQuery = "SELECT asiakas_id, etunimi, sukunimi FROM asiakas";
                    MySqlDataAdapter asiakasAdapter = new MySqlDataAdapter(asiakasnimetQuery, connection);
                    DataTable asiakasDt = new DataTable();
                    asiakasAdapter.Fill(asiakasDt);

                    var asiakasData = asiakasDt.AsEnumerable()
                        .Select(row => new
                        {
                            asiakas_id = row.Field<uint>("asiakas_id"),
                            FullName = row.Field<string>("etunimi") + " " + row.Field<string>("sukunimi"),
                            DisplayText = row.Field<string>("etunimi") + " " + row.Field<string>("sukunimi") + " (" + row.Field<uint>("asiakas_id") + ")"
                        })
                        .ToList();

                    cmbUusiVarausValitseAsiakas.DisplayMember = "DisplayText";
                    cmbUusiVarausValitseAsiakas.ValueMember = "asiakas_id";
                    cmbUusiVarausValitseAsiakas.DataSource = asiakasData;


                    // täyttää mokki_id comboboxin
                    string mokkinimiQuery = "SELECT mokki_id, mokkinimi FROM mokki";
                    MySqlDataAdapter mokkiAdapter = new MySqlDataAdapter(mokkinimiQuery, connection);
                    DataSet mokkiDs = new DataSet();
                    mokkiAdapter.Fill(mokkiDs, "mokki");

                    var mokkiData = mokkiDs.Tables["mokki"].AsEnumerable()
                        .Select(row => new
                        {
                            mokki_id = row.Field<uint>("mokki_id"),
                            mokkinimi = row.Field<string>("mokkinimi"),
                            DisplayText = row.Field<string>("mokkinimi") + " (" + row.Field<uint>("mokki_id") + ")"
                        })
                        .ToList();

                    cmbUusiVarausValitseMokki.DisplayMember = "DisplayText";
                    cmbUusiVarausValitseMokki.ValueMember = "mokki_id";
                    cmbUusiVarausValitseMokki.DataSource = mokkiData;
                }
                catch (Exception ex)
                {
                    // tietokantaan ei ole lisätty mökkejä, tai asiakkaita.
                    MessageBox.Show("Varausta ei voi tehdä, koska tietokannasta luultavasti puuttuu mökin ja/tai asiakkaan tiedot\n " + ex.ToString()); ;
                    return;
                }
            } // täyttää comboboxit asiakkailla ja mökeillä
            string selectQuery = "SELECT * FROM varaus";
            DataTable datatable = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter(selectQuery, connection);
            dgMokkiVaraukset.DataSource = datatable;
            adapter.Fill(datatable);
            connection.Close();
        }
        private void Form_Shown(object sender, EventArgs e)
        {
            dgMokkiVaraukset.ClearSelection();
            if (cmbUusiVarausValitseMokki.Text == "" || cmbUusiVarausValitseAsiakas.Text == "")
            {
                MessageBox.Show("Asiakkaita ja/tai mökkejä ei ole lisätty tietokantaan, joten varauksia ei voi tehdä.\n" +
                    "Lisää ensin asiakkaat asiakashallinnan kautta, ja lisää tarvittaessa mökkejä mökkienhallinnan kautta.");
            }
            tbUusiVarausVarausID.Visible = false;
        } // formi avautuu
        private void btnValmisVaraus_Click(object sender, EventArgs e)
        {
            if (!muokkausMenossa)
            {
                if (Convert.ToDateTime(dateTimeMokinVarausLoppuPvm.Text) > DateTime.Now) // varauksen loppupvm ei voi olla menneisyydessä
                {
                    DateTime varattuloppupvm = Convert.ToDateTime(dateTimeMokinVarausLoppuPvm.Text);
                    if (Convert.ToDateTime(dateTimeMokinVarausAlkuPvm.Text) > DateTime.Now) //varauksen alkupvm ei voi olla menneisyydessä
                    {
                        DateTime varattualkupvm = Convert.ToDateTime(dateTimeMokinVarausAlkuPvm.Text);

                        if (varattuloppupvm > varattualkupvm) //varauksen loppupvm ei voi olla ennen alkupvm:ää
                        {
                            int asiakasid = Convert.ToInt32(cmbUusiVarausValitseAsiakas.SelectedValue);
                            int mokkimokkiid = Convert.ToInt32(cmbUusiVarausValitseMokki.SelectedValue);
                            try
                            {
                                //int varausid = int.Parse(tbUusiVarausVarausID.Text);
                                using (connection)
                                {
                                    connection.Open();

                                    string paallekkaisVarausQuery = "SELECT COUNT(*) " +
                                                              "FROM varaus " +
                                                              "WHERE mokki_mokki_id = @mokkimokkiid " +
                                                              "AND varattu_alkupvm <= @varattuloppupvm " +
                                                              "AND varattu_loppupvm >= @varattualkupvm;";

                                    using (MySqlCommand checkCommand = new MySqlCommand(paallekkaisVarausQuery, connection))
                                    {
                                        checkCommand.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                                        checkCommand.Parameters.AddWithValue("@varattualkupvm", varattualkupvm);
                                        checkCommand.Parameters.AddWithValue("@varattuloppupvm", varattuloppupvm);

                                        int overlappingCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                                        if (overlappingCount > 0)
                                        {
                                            MessageBox.Show("Tätä varausta ei voi suorittaa. Päivämäärät menevät päällekkäin aiemman varauksen kanssa.");
                                            return;
                                        }
                                    }

                                    string VarauksentiedotInsertQuery = "INSERT INTO varaus(asiakas_id, mokki_mokki_id, varattu_pvm, vahvistus_pvm, " +
                                        "varattu_alkupvm, varattu_loppupvm)" +
                                    "VALUES (@asiakasid, @mokkimokkiid, @varattupvm, @vahvistuspvm, @varattualkupvm, @varattuloppupvm)";

                                    using (MySqlCommand command = new MySqlCommand(VarauksentiedotInsertQuery, connection))
                                    {
                                        //command.Parameters.AddWithValue("@varausid", varausid);
                                        command.Parameters.AddWithValue("@asiakasid", asiakasid);
                                        command.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                                        command.Parameters.AddWithValue("@varattupvm", DateTime.Now);
                                        command.Parameters.AddWithValue("@vahvistuspvm", vahvistuspvm);
                                        command.Parameters.AddWithValue("@varattualkupvm", varattualkupvm);
                                        command.Parameters.AddWithValue("@varattuloppupvm", varattuloppupvm);
                                        command.ExecuteNonQuery();
                                    }
                                    connection.Close();
                                }
                                UpdatedgMokkiVaraukset();
                                btnValmisVaraus.Enabled = false;
                            }
                            catch
                            {
                                MessageBox.Show("Varauksen tiedot ovat puutteelliset. Tarkasta VarausID, sekä varattu mökki, ja varaajan nimi.");
                                btnValmisVaraus.Enabled = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Valitettavasti varauksen täytyy kestää vähintään yksi vuorokausi");
                            btnValmisVaraus.Enabled = false;
                        }

                    }
                    else
                    {
                        MessageBox.Show("Ei voida lisätä varausta, jonka alkupäivä olisi menneisyydessä/varauksentekopäivänä");
                        btnValmisVaraus.Enabled = false;
                    }
                }
                else
                {
                    MessageBox.Show("Ei voida lisätä varausta, jonka loppupäivä on menneisyydessä/varauksentekopäivänä");
                    btnValmisVaraus.Enabled = false;
                }
            }
            else //Jos muokkaustila on päällä
            {
                //tbUusiVarausVarausID.Enabled = true;
                btnMuokkaaMokkiVarausta.Text = "Muokkaa varausta";
                dgMokkiVaraukset.ClearSelection();
                muokkausMenossa = false;
                btnValmisVaraus.Text = "Tallenna Varaus";
                btnMokinVarausVahvista.Text = "Vahvista varauksen tiedot";
                btnPoistaMokkiVaraus.Visible = true;
                if (Convert.ToDateTime(dateTimeMokinVarausLoppuPvm.Text) > DateTime.Now) // varauksen loppupvm ei voi olla menneisyydessä
                {
                    DateTime varattuloppupvm = Convert.ToDateTime(dateTimeMokinVarausLoppuPvm.Text);
                    if (Convert.ToDateTime(dateTimeMokinVarausAlkuPvm.Text) > DateTime.Now) //varauksen alkupvm ei voi olla menneisyydessä
                    {
                        DateTime varattualkupvm = Convert.ToDateTime(dateTimeMokinVarausAlkuPvm.Text);

                        if (varattuloppupvm > varattualkupvm) //varauksen loppupvm ei voi olla ennen alkupvm:ää
                        {
                            int asiakasid = Convert.ToInt32(cmbUusiVarausValitseAsiakas.SelectedValue);
                            int mokkimokkiid = Convert.ToInt32(cmbUusiVarausValitseMokki.SelectedValue);
                            try
                            {

                                int varausid = int.Parse(tbUusiVarausVarausID.Text);
                                using (connection)
                                {
                                    connection.Open();

                                    string paallekkaisVarausQuery = "SELECT COUNT(*) " +
                                                              "FROM varaus " +
                                                              "WHERE mokki_mokki_id = @mokkimokkiid " +
                                                              "AND varattu_alkupvm <= @varattuloppupvm " +
                                                              "AND varattu_loppupvm >= @varattualkupvm " +
                                                              "AND varaus_id != @varausid";

                                    using (MySqlCommand checkCommand = new MySqlCommand(paallekkaisVarausQuery, connection))
                                    {
                                        checkCommand.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                                        checkCommand.Parameters.AddWithValue("@varattualkupvm", varattualkupvm);
                                        checkCommand.Parameters.AddWithValue("@varattuloppupvm", varattuloppupvm);
                                        checkCommand.Parameters.AddWithValue("@varausid", varausid);
                                        int overlappingCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                                        if (overlappingCount > 0)
                                        {
                                            MessageBox.Show("Tätä varausta ei voi suorittaa. Päivämäärät menevät päällekkäin toisen varauksen kanssa.");
                                            return;
                                        }
                                    }
                                    string VarauksentiedotUpdateQuery = "UPDATE varaus SET asiakas_id = @asiakasid, mokki_mokki_id = @mokkimokkiid, vahvistus_pvm = @vahvistuspvm, varattu_alkupvm = @varattualkupvm, " +
                                    "varattu_loppupvm = @varattuloppupvm WHERE varaus_id = @varausid";

                                    using (MySqlCommand command = new MySqlCommand(VarauksentiedotUpdateQuery, connection))
                                    {
                                        command.Parameters.AddWithValue("@varausid", varausid);
                                        command.Parameters.AddWithValue("@asiakasid", asiakasid);
                                        command.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                                        command.Parameters.AddWithValue("@vahvistuspvm", vahvistuspvm);
                                        command.Parameters.AddWithValue("@varattualkupvm", varattualkupvm);
                                        command.Parameters.AddWithValue("@varattuloppupvm", varattuloppupvm);
                                        command.ExecuteNonQuery();
                                    }
                                    connection.Close();
                                }
                                UpdatedgMokkiVaraukset();
                                btnValmisVaraus.Enabled = false;
                            }
                            catch
                            {
                                MessageBox.Show("Varaus_ID on virheellinen");
                                btnValmisVaraus.Enabled = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Valitettavasti varauksen täytyy kestää vähintään yksi vuorokausi");
                            btnValmisVaraus.Enabled = false;
                        }

                    }
                    else
                    {
                        MessageBox.Show("Ei voida lisätä varausta, jonka alkupäivä olisi menneisyydessä/varauksentekopäivänä");
                        btnValmisVaraus.Enabled = false;
                    }
                }
                else
                {
                    MessageBox.Show("Ei voida lisätä varausta, jonka loppupäivä on menneisyydessä/varauksentekopäivänä");
                    btnValmisVaraus.Enabled = false;
                }
            }
        } //lisää annetut varaustiedot tietokantaan
        private void UpdatedgMokkiVaraukset()
        {
            //Varauksen hallinnan datagridviewiin tietojen vienti
            string selectQuery = "SELECT * FROM varaus";
            DataTable datatable = new DataTable();
            using (connection)
            {
                using (MySqlCommand command = new MySqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    adapter.Fill(datatable);
                    connection.Close();
                }
            }
            dgMokkiVaraukset.DataSource = datatable;
        } // päivittää mökkivarauslistan
        private void btnMokinVarausVahvista_Click(object sender, EventArgs e)
        {
            vahvistuspvm = DateTime.Now;
            btnValmisVaraus.Enabled = true;
        }//Käyttäjä 'vahvistaa' varauksen
        private void btnTakaisinMokkiFormiin_Click(object sender, EventArgs e)
        {
            Form mokit = new FormMokitJaMokkivaraukset();
            this.Hide();
            mokit.ShowDialog();
            this.Close();
        }//avaa mökkienhallinnan
        private void btnPoistaMokkiVaraus_Click(object sender, EventArgs e)
        {
            if (!haunRajausPaalla == true)
            {
                if (dgMokkiVaraukset.SelectedRows.Count > 0)
                {
                    bool tyhjarivi = true;
                    foreach (DataGridViewCell cell in dgMokkiVaraukset.SelectedCells)
                    {
                        if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                        {
                            tyhjarivi = false;
                        }
                    }
                    if (!tyhjarivi)
                    {
                        DialogResult result = MessageBox.Show("Haluatko varmasti poistaa valitun varauksen tietokannasta?", "Oletko varma?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result == DialogResult.Yes)
                        {
                            int selectedIndex = dgMokkiVaraukset.SelectedRows[0].Index;
                            int varausid = int.Parse(dgMokkiVaraukset[0, selectedIndex].Value.ToString());
                            string TarkastaMahdollisetLaskutVaraukselle = "SELECT * FROM lasku WHERE varaus_id = @varausid";
                            string TarkastaMahdollisetVarauksenPalvelutVaraukselle = "SELECT * FROM varauksen_palvelut WHERE varaus_id = @varausid";
                            using (connection)
                            {
                                using (MySqlCommand checklaskutCommand = new MySqlCommand(TarkastaMahdollisetLaskutVaraukselle, connection))
                                {
                                    checklaskutCommand.Parameters.AddWithValue("@varausid", varausid);
                                    connection.Open();
                                    MySqlDataReader reader = checklaskutCommand.ExecuteReader();
                                    if (!reader.HasRows) // true = Varauksesta ei ole tehty laskua
                                    {
                                        reader.Close();
                                        using (MySqlCommand checkvarauksenpalvelutCommand = new MySqlCommand(TarkastaMahdollisetVarauksenPalvelutVaraukselle, connection))
                                        {
                                            checkvarauksenpalvelutCommand.Parameters.AddWithValue("@varausid", varausid);
                                            reader = checkvarauksenpalvelutCommand.ExecuteReader();
                                            if (!reader.HasRows) // true = Varaukseen ei liity varauksen_palvelut -tablen tietoja
                                            {
                                                reader.Close();
                                                string PoistaVarauksenTiedotQuery = "DELETE FROM varaus WHERE varaus_id = @varausid";
                                                using (MySqlCommand command = new MySqlCommand(PoistaVarauksenTiedotQuery, connection))
                                                {
                                                    command.Parameters.AddWithValue("@varausid", varausid);
                                                    command.ExecuteNonQuery();
                                                }
                                                connection.Close();
                                                UpdatedgMokkiVaraukset();
                                                tbUusiVarausVarausID.Text = "";
                                            }
                                            else
                                            {
                                                MessageBox.Show("Varaukseen on liitetty palveluita. Varauksen_palveluista täytyy ensin poistaa tällä varaus_id:llä lisätyt tiedot.");
                                            }
                                            connection.Close();
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("Varauksesta on tehty lasku tietokantaan. Lasku täytyy poistaa ensin, jotta varauksen voi poistaa.");
                                    }
                                    connection.Close();
                                }
                            }
                        }
                    }
                    else
                        MessageBox.Show("Olet valinnut tyhjän rivin. Sitä ei voi poistaa (ei ole mitään poistettavaa).");
                }
                else
                {
                    MessageBox.Show("Valitse varaus, jonka haluat poistaa. Kokeile sitten uudelleen.");
                }
            }
            else
                MessageBox.Show("Suorita ensin haun rajaus loppuun");

            
        } //poistaa valitun varauksen tiedot

        private void btnMuokkaaMokkiVarausta_Click(object sender, EventArgs e)
        {
            if (!haunRajausPaalla == true)
            {
                muokkausMenossa = true;
                if (dgMokkiVaraukset.SelectedRows.Count > 0)
                {
                    bool tyhjarivi = true;
                    foreach (DataGridViewCell cell in dgMokkiVaraukset.SelectedCells)
                    {
                        if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                        {
                            tyhjarivi = false;
                        }
                    }
                    if (!tyhjarivi)
                    {
                        btnMuokkaaMokkiVarausta.Text = "Muokkaus käynnissä...";
                        muokkausMenossa = true;
                        btnValmisVaraus.Text = "Tallenna muutokset";
                        btnMokinVarausVahvista.Text = "Vahvista muutokset";
                        btnPoistaMokkiVaraus.Visible = false;
                        btnValmisVaraus.Enabled = false;
                        tbUusiVarausVarausID.Enabled = false;
                        tbUusiVarausVarausID.Text = dgMokkiVaraukset.CurrentRow.Cells[0].Value.ToString();
                        dateTimeMokinVarausAlkuPvm.Value = DateTime.Parse(dgMokkiVaraukset.CurrentRow.Cells[5].Value.ToString());
                        dateTimeMokinVarausLoppuPvm.Value = DateTime.Parse(dgMokkiVaraukset.CurrentRow.Cells[6].Value.ToString());
                    }
                    else
                        MessageBox.Show("Olet valinnut tyhjän rivin. Sitä ei voi muokata (ei ole mitään muokattavaa).");
                }
            }
            else
                MessageBox.Show("Suorita ensin haun rajaus loppuun.");
            
        } //antaa muokata valitun varauksen tietoja
        private void btmRajaaVarausHakua_Click(object sender, EventArgs e)
        {
            if (!haunRajausPaalla)
            {
                haunRajausPaalla = true;
                panelHaunRajaus.Location = new Point(0, 40);
                panelHaunRajaus.Visible = true;
                btnRajaaVarausHakua.Text = "TEE HAKU";
                cmbUusiVarausValitseAsiakas.Enabled = false;
                cmbUusiVarausValitseMokki.Enabled = false;
                panelHakuOhjeet.Visible = true;
                panelHakuOhjeet.Location = new Point(7, 105);
                btnMokinVarausVahvista.Visible = false;
                btnValmisVaraus.Visible = false;
                lblPvmOhje.Visible = false;
            }
            else
            {
                haunRajausPaalla = false;
                panelHaunRajaus.Location = new Point(7, 221);
                panelHaunRajaus.Visible = false;
                btnRajaaVarausHakua.Text = "Rajaa hakua";
                
                try
                {
                    if (cmbUusiVarausValitseAsiakas.Enabled && cmbUusiVarausValitseMokki.Enabled)
                    {
                        int asiakasid = Convert.ToInt32(cmbUusiVarausValitseAsiakas.SelectedValue);
                        try
                        {
                            if (!(cmbUusiVarausValitseAsiakas.Text == "") || !(cmbUusiVarausValitseMokki.Text == ""))
                            {
                                string mokkimokkiid = cmbUusiVarausValitseMokki.SelectedValue.ToString();
                                string hakuQuery = "SELECT * FROM varaus WHERE asiakas_id = @asiakasid AND mokki_mokki_id = @mokkimokkiid";
                                DataTable datatable = new DataTable();
                                using (connection)
                                {
                                    MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                                    command.Parameters.AddWithValue("@asiakasid", asiakasid);
                                    command.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                                    adapter.Fill(datatable);
                                    connection.Close();
                                }
                                dgMokkiVaraukset.DataSource = datatable;


                                checkMokkiNimiRajaus.Checked = false;
                                checkVaraajaNimiRajaus.Checked = false;
                            }
                            else
                            {
                                MessageBox.Show("Hakukriteerit puutteelliset. Mahdollinen syy: mokki ja/tai asiakas -taulut ovat tyhjiä");
                                checkMokkiNimiRajaus.Checked = false;
                                checkVaraajaNimiRajaus.Checked = false;
                            }

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Haku ei onnistunut\n\n" + ex);
                            checkMokkiNimiRajaus.Checked = false;
                            checkVaraajaNimiRajaus.Checked = false;
                        }


                    }
                    else if (cmbUusiVarausValitseAsiakas.Enabled && !cmbUusiVarausValitseMokki.Enabled)
                    {
                        int asiakasid = Convert.ToInt32(cmbUusiVarausValitseAsiakas.SelectedValue);

                        string hakuQuery = "SELECT * FROM varaus WHERE asiakas_id = @asiakasid";
                        DataTable datatable = new DataTable();
                        using (connection)
                        {
                            MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                            command.Parameters.AddWithValue("@asiakasid", asiakasid);
                            MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                            adapter.Fill(datatable);
                            connection.Close();
                        }
                        dgMokkiVaraukset.DataSource = datatable;

                        checkMokkiNimiRajaus.Checked = false;
                        checkVaraajaNimiRajaus.Checked = false;
                    }
                    else if (!cmbUusiVarausValitseAsiakas.Enabled && cmbUusiVarausValitseMokki.Enabled && !(cmbUusiVarausValitseMokki.Text == ""))
                    {
                        string mokkimokkiid = cmbUusiVarausValitseMokki.SelectedValue.ToString();

                        string hakuQuery = "SELECT * FROM varaus WHERE mokki_mokki_id = @mokkimokkiid";
                        DataTable datatable = new DataTable();
                        using (connection)
                        {
                            MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                            command.Parameters.AddWithValue("@mokkimokkiid", mokkimokkiid);
                            MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                            adapter.Fill(datatable);
                            connection.Close();
                        }
                        dgMokkiVaraukset.DataSource = datatable;

                        checkMokkiNimiRajaus.Checked = false;
                        checkVaraajaNimiRajaus.Checked = false;
                    }
                    else
                    {
                        UpdatedgMokkiVaraukset();
                        checkMokkiNimiRajaus.Checked = false;
                        checkVaraajaNimiRajaus.Checked = false;
                    }
                    cmbUusiVarausValitseAsiakas.Enabled = true;
                    cmbUusiVarausValitseMokki.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Haussa tapahtui seuraava virhe:\n\n" + ex);
                    checkMokkiNimiRajaus.Checked = false;
                    checkVaraajaNimiRajaus.Checked = false;
                }
                cmbUusiVarausValitseAsiakas.Enabled = true;
                cmbUusiVarausValitseMokki.Enabled = true;
                panelHakuOhjeet.Visible = false;
                panelHakuOhjeet.Location = new Point(332, 117);
                btnMokinVarausVahvista.Visible = true;
                btnValmisVaraus.Visible = true;
                lblPvmOhje.Visible = true;
            }
        } //antaa hakea varauksia eri hakukriteereillä

        private void checkMokkiNimiRajaus_CheckedChanged(object sender, EventArgs e)
        {
            if(checkMokkiNimiRajaus.Checked == true)
            {
                cmbUusiVarausValitseMokki.Enabled = true;
            }
            else
                cmbUusiVarausValitseMokki.Enabled = false;
        } //mökin nimen perusteella hakeminen pois päältä / päälle

        private void checkVaraajaNimiRajaus_CheckedChanged(object sender, EventArgs e)
        {
            if(checkVaraajaNimiRajaus.Checked == true)
            {
                cmbUusiVarausValitseAsiakas.Enabled = true;
            }
            else
                cmbUusiVarausValitseAsiakas.Enabled = false;

        } // varaajan nimen perusteella hakeminen pois päältä / päälle

        private void btnAsiakasHallintaan_Click(object sender, EventArgs e)
        {
            var asiakkaat = new FormAsiakashallinta();
            this.Hide();
            asiakkaat.ShowDialog();
            this.Close();
            
        } // avaa asiakashallinnan, jotta asiakkaita voi lisätä järjestelmään
    }
}
