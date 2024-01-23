using iText.Layout.Splitting;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mokkivarausjarjestelma
{
    public partial class FormMokitJaMokkivaraukset : Form
    {
        private bool muokkausMenossa = false; //kertoo ohjelmalle, onko mökin tietojen muokkaus menossa
        private bool hakuPaalla = false; // kertoo ohjelmalle, onko käyttäjä suorittamassa rajattua hakua
        private bool tyhjaLista;
        MySqlConnection connection = new MySqlConnection("datasource=localhost;port=3307;Initial Catalog='vn';username=root;password=Ruutti"); //yhteys tietokantaan
        public FormMokitJaMokkivaraukset()
        {
            InitializeComponent();
            dgMokkiLista.AutoGenerateColumns = true;
            UpdatedgMokkiLista();
            this.Shown += Form_Shown; // ilman tätä dgv:n tietorivi on automaattisesti valittuna, kun käyttäjä avaa formin
            btnMuokkaaValitunMokinTietoja.Enabled = false;
            
        } //toiminnot, jotka toteutuvat formin avautuessa
        private void Form_Shown(object sender, EventArgs e)
        {
            dgMokkiLista.ClearSelection();
            
            using (connection)
            {
                try
                {
                    // täyttää alue_id comboboxin
                    string aluenimiQuery = "SELECT alue_id, nimi FROM alue";
                    MySqlDataAdapter alueNimiAdapter = new MySqlDataAdapter(aluenimiQuery, connection);
                    DataSet alueDs = new DataSet();
                    alueNimiAdapter.Fill(alueDs, "alue");

                    var aluenimiData = alueDs.Tables["alue"].AsEnumerable()
                        .Select(row => new
                        {
                            alue_id = row.Field<uint>("alue_id"),
                            nimi = row.Field<string>("nimi"),
                            DisplayText = row.Field<string>("nimi") + " (" + row.Field<uint>("alue_id") + ")"
                        })
                        .ToList();

                    cmbUusiMokkiValitseAlueID.DisplayMember = "DisplayText";
                    cmbUusiMokkiValitseAlueID.ValueMember = "alue_id";
                    cmbUusiMokkiValitseAlueID.DataSource = aluenimiData;
                    connection.Close();
                    connection.Open();

                    // täyttää postinumero comboboxin
                    string postinroQuery = "SELECT postinro FROM posti";
                    MySqlDataAdapter postiAdapter = new MySqlDataAdapter(postinroQuery, connection);
                    DataSet postiDs = new DataSet();
                    postiAdapter.Fill(postiDs, "posti");

                    cmbUusiMokkiValitsePostiNro.DisplayMember = "postinro";
                    cmbUusiMokkiValitsePostiNro.ValueMember = "postinro";
                    cmbUusiMokkiValitsePostiNro.DataSource = postiDs.Tables["posti"];
                    connection.Close();
                    connection.Open();
                    tyhjaLista = false;
                }
                catch (Exception ex)
                {
                    // tietokantaan ei ole lisätty alueita, tai postitoimipaikkoja.
                    MessageBox.Show("VIRHE: " + ex);
                    connection.Close();
                }

            }
            if (cmbUusiMokkiValitseAlueID.Text == "" || cmbUusiMokkiValitsePostiNro.Text == "")
            {
                btnHaeMokit.Enabled = false;
                tyhjaLista = true;
                MessageBox.Show("Tietokannasta puuttuu alueen ja/tai postitoimipaikan tiedot\nTästä johtuen et voi lisätä uusia mökkejä tietokantaan.\nLisää alueita ja postitoimipaikkoja Aluehallinnan kautta, jos haluat lisätä mökkejä tietokantaan.");
            }
        } // ilman tätä dgv:n tietorivi on automaattisesti valittuna, kun käyttäjä avaa formin. Etsii alue-taulun oliot yhteen comboboxiin, ja posti-taulun oliot toiseen, taikka varoittaa käyttäjää, mikäli posti- ja alue-taulut ovat tyhjiä formin avautuessa.
        private void btnUusiVaraus_Click(object sender, EventArgs e)
        {
            var VarausForm = new FormVaraus();
            this.Hide();
            VarausForm.ShowDialog();
            this.Close();
        } //käyttäjä siirtyy mökkivarausten hallintaan
        private void UpdatedgMokkiLista()
        {
            // Mökkivarausten hallinnan datagridviewiin tietojen vienti
            string selectQuery = "SELECT * FROM mokki";
            DataTable datatable = new DataTable();
            using (connection)
            {
                MySqlCommand command = new MySqlCommand(selectQuery, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                adapter.Fill(datatable);
                connection.Close();
            }
            dgMokkiLista.DataSource = datatable;
        } //mökkilista datagridview päivittyy
        private void btnLisaaMokinTiedot_Click(object sender, EventArgs e)
        {
            if (btnLisaaMokinTiedot.Text == "Lisää mökin tiedot")
            {
                if (ValidateTexts())
                {
                    int alueid = Convert.ToInt32(cmbUusiMokkiValitseAlueID.SelectedValue);
                    string postinro = cmbUusiMokkiValitsePostiNro.SelectedValue.ToString();

                    //int mokkiid = int.Parse(tbValittuMokkiMokkiID.Text);
                    string mokkinimi = tbValittuMokkiNimi.Text.ToString();
                    string katuosoite = tbValittuMokkiOsoite.Text.ToString();
                    double hinta = double.Parse(tbValittuMokkiHintaVrk.Text);
                    string mokinkuvaus = rtbValittuMokkiKuvaus.Text.ToString();
                    int hlomaara = int.Parse(tbValittuMokkiHloMaara.Text);
                    string mokinvarustelu = rtbValittuMokkiVarustelu.Text.ToString();
                    //string MokintiedotInsertQuery = "INSERT INTO mokki(mokki_id, alue_id, postinro, mokkinimi, katuosoite, hinta, kuvaus, henkilomaara, varustelu) VALUES (@mokkiid, @alueid, @postinro, @mokkinimi, @katuosoite, @hinta, @mokinkuvaus, @hlomaara, @mokinvarustelu)";
                    string MokintiedotInsertQuery = "INSERT INTO mokki(alue_id, postinro, mokkinimi, katuosoite, hinta, kuvaus, henkilomaara, varustelu) VALUES (@alueid, @postinro, @mokkinimi, @katuosoite, @hinta, @mokinkuvaus, @hlomaara, @mokinvarustelu)";

                    using (connection)
                    {
                        using (MySqlCommand command = new MySqlCommand(MokintiedotInsertQuery, connection))
                        {
                            //command.Parameters.AddWithValue("@mokkiid", mokkiid);
                            command.Parameters.AddWithValue("@alueid", alueid);
                            command.Parameters.AddWithValue("@postinro", postinro);
                            command.Parameters.AddWithValue("@mokkinimi", mokkinimi);
                            command.Parameters.AddWithValue("@katuosoite", katuosoite);
                            command.Parameters.AddWithValue("@hinta", hinta);
                            command.Parameters.AddWithValue("@mokinkuvaus", mokinkuvaus);
                            command.Parameters.AddWithValue("@hlomaara", hlomaara);
                            command.Parameters.AddWithValue("@mokinvarustelu", mokinvarustelu);
                            connection.Open();
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (MySqlException ex)
                            {
                                if (ex.Number == 1062) // ID on jo käytössä
                                {
                                    MessageBox.Show("Mökki ID on jo olemassa. Valitse uusi ID.");
                                }
                                else
                                {
                                    MessageBox.Show("Virhe tiedonsiirrossa: " + ex.Message);
                                }
                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                    UpdatedgMokkiLista();
                    ClearTextBoxes();
                }
                else
                {
                    MessageBox.Show("Tarkasta tekstikenttien täyttö");
                }
            }
            else if (btnLisaaMokinTiedot.Text == "Tyhjennä tekstikentät")
            {
                ClearTextBoxes();
                tbValittuMokkiNimi.ReadOnly = false;
                tbValittuMokkiOsoite.ReadOnly = false;
                tbValittuMokkiHintaVrk.ReadOnly = false;
                rtbValittuMokkiKuvaus.ReadOnly = false;
                tbValittuMokkiHloMaara.ReadOnly = false;
                rtbValittuMokkiVarustelu.ReadOnly = false;
                muokkausMenossa = false;
                btnLisaaMokinTiedot.Text = "Lisää mökin tiedot";
                btnMuokkaaValitunMokinTietoja.Enabled = false;
                cmbUusiMokkiValitseAlueID.Enabled = true;
                cmbUusiMokkiValitsePostiNro.Enabled = true;
            }
        } // käyttäjä lisää uudet mökin tiedot järjestelmään
        private void ClearTextBoxes()
        {
            tbValittuMokkiNimi.Clear();
            tbValittuMokkiOsoite.Clear();
            tbValittuMokkiHintaVrk.Clear();
            rtbValittuMokkiKuvaus.Clear();
            tbValittuMokkiHloMaara.Clear();
            rtbValittuMokkiVarustelu.Clear();
        } // tyhjentää tekstikentät uuden mökin lisäystä varten
        private void dgMokkiLista_SelectionChanged(object sender, EventArgs e)
        {
            if (!hakuPaalla)
            {
                if (dgMokkiLista.SelectedRows.Count > 0 && dgMokkiLista.Focused && !muokkausMenossa && !tyhjaLista)
                {
                    try
                    {
                        cmbUusiMokkiValitseAlueID.Enabled = false;
                        cmbUusiMokkiValitsePostiNro.Enabled = false;
                        tbValittuMokkiNimi.ReadOnly = true;
                        tbValittuMokkiOsoite.ReadOnly = true;
                        tbValittuMokkiHintaVrk.ReadOnly = true;
                        rtbValittuMokkiKuvaus.ReadOnly = true;
                        tbValittuMokkiHloMaara.ReadOnly = true;
                        rtbValittuMokkiVarustelu.ReadOnly = true;

                        cmbUusiMokkiValitseAlueID.SelectedValue = uint.Parse(dgMokkiLista.CurrentRow.Cells[1].Value.ToString());
                        cmbUusiMokkiValitsePostiNro.SelectedValue = dgMokkiLista.CurrentRow.Cells[2].Value.ToString();
                        tbValittuMokkiNimi.Text = dgMokkiLista.CurrentRow.Cells[3].Value.ToString();
                        tbValittuMokkiOsoite.Text = dgMokkiLista.CurrentRow.Cells[4].Value.ToString();
                        tbValittuMokkiHintaVrk.Text = dgMokkiLista.CurrentRow.Cells[5].Value.ToString();
                        rtbValittuMokkiKuvaus.Text = dgMokkiLista.CurrentRow.Cells[6].Value.ToString();
                        tbValittuMokkiHloMaara.Text = dgMokkiLista.CurrentRow.Cells[7].Value.ToString();
                        rtbValittuMokkiVarustelu.Text = dgMokkiLista.CurrentRow.Cells[8].Value.ToString();
                        btnMuokkaaValitunMokinTietoja.Enabled = true;
                        btnLisaaMokinTiedot.Text = "Tyhjennä tekstikentät";
                    }
                    catch
                    {
                        MessageBox.Show("Virhe rivin valinnassa");
                    }
                }
                else
                {
                    btnLisaaMokinTiedot.Text = "Lisää mökin tiedot";
                }
            }
            else
            {
                return;
            }            
        } // kun käyttäjä klikkaa mökkiä listalta, mökin tiedot siirtyvät tekstikenttiin
        private void btnPoistaValittuMokkiListalta_Click(object sender, EventArgs e)
        {
            
            if (!hakuPaalla)
            {
                if (dgMokkiLista.SelectedRows.Count > 0)
                {
                    bool tyhjarivi = true;
                    foreach(DataGridViewCell cell in dgMokkiLista.SelectedCells)
                    {
                        if(cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                        {
                            tyhjarivi = false;
                        }
                    }
                    if (!tyhjarivi)
                    {
                        DialogResult result = MessageBox.Show("Haluatko varmasti poistaa valitun mökin tietokannasta?", "Oletko varma?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result == DialogResult.Yes)
                        {
                            int selectedIndex = dgMokkiLista.SelectedRows[0].Index;
                            int mokkiid = int.Parse(dgMokkiLista[0, selectedIndex].Value.ToString());
                            string TarkastaMahdollisetVarauksetMokilleQuery = "SELECT * FROM varaus WHERE mokki_mokki_id = @mokkiid";

                            using (connection)
                            {
                                using (MySqlCommand checkCommand = new MySqlCommand(TarkastaMahdollisetVarauksetMokilleQuery, connection))
                                {
                                    checkCommand.Parameters.AddWithValue("@mokkiid", mokkiid);
                                    connection.Open();
                                    MySqlDataReader reader = checkCommand.ExecuteReader();

                                    if (!reader.HasRows) // true = Mökille ei varauksia, joten sen voi poistaa
                                    {
                                        reader.Close();
                                        string PoistaMokinTiedotQuery = "DELETE FROM mokki WHERE mokki_id = @mokkiid";
                                        using (MySqlCommand command = new MySqlCommand(PoistaMokinTiedotQuery, connection))
                                        {
                                            command.Parameters.AddWithValue("@mokkiid", mokkiid);
                                            command.ExecuteNonQuery();
                                        }
                                        UpdatedgMokkiLista();
                                        ClearTextBoxes();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Mökki on varattu. Sitä ei voi poistaa tietokannasta. Varaus täytyy poistaa ensin.");
                                    }
                                    connection.Close();
                                }
                            }
                        }
                    }
                    else
                        MessageBox.Show("Et voi poistaa tyhjää riviä");
                    
                }
                else
                {
                    MessageBox.Show("Valitse mökki, jonka haluat poistaa. Kokeile sitten uudelleen.");
                }
            }
            else
            {
                MessageBox.Show("Laita ensin haku pois päältä 'Lopeta'-napista");
            }
            
        } // poistaa valitun mökin tietokannasta, mikäli tietyt ehdot täyttyvät
        private void btnTakaisinAloitusFormiin_Click(object sender, EventArgs e)
        {
            Form formaloitus = new Form1();
            this.Hide();

            formaloitus.ShowDialog();
            this.Close();
        } //palaa aloitusformiin
        private void btnMuokkaaValitunMokinTietoja_Click(object sender, EventArgs e)
        {
            
            if (!hakuPaalla)
            {
                if (!muokkausMenossa)
                {
                    // muokkaustila
                    btnLisaaMokinTiedot.Enabled = false;
                    btnMuokkaaValitunMokinTietoja.Text = "Valmis";
                    cmbUusiMokkiValitseAlueID.Enabled = true;
                    cmbUusiMokkiValitsePostiNro.Enabled = true;
                    tbValittuMokkiNimi.ReadOnly = false;
                    tbValittuMokkiOsoite.ReadOnly = false;
                    tbValittuMokkiHintaVrk.ReadOnly = false;
                    rtbValittuMokkiKuvaus.ReadOnly = false;
                    tbValittuMokkiHloMaara.ReadOnly = false;
                    rtbValittuMokkiVarustelu.ReadOnly = false;
                    muokkausMenossa = true;
                }
                else
                {
                    if (ValidateTexts())
                    {
                        // tietokanta ja dgv päivittyvät
                        UpdateDatabaseAndDataGridView();
                        // pois muokkaustilasta
                        btnLisaaMokinTiedot.Enabled = true;
                        btnLisaaMokinTiedot.Text = "Tyhjennä tekstikentät";
                        btnMuokkaaValitunMokinTietoja.Text = "Muokkaa";
                        cmbUusiMokkiValitseAlueID.Enabled = false;
                        cmbUusiMokkiValitsePostiNro.Enabled = false;
                        tbValittuMokkiNimi.ReadOnly = true;
                        tbValittuMokkiOsoite.ReadOnly = true;
                        tbValittuMokkiHintaVrk.ReadOnly = true;
                        rtbValittuMokkiKuvaus.ReadOnly = true;
                        tbValittuMokkiHloMaara.ReadOnly = true;
                        rtbValittuMokkiVarustelu.ReadOnly = true;
                        // Set ReadOnly properties to true for all textboxes
                        muokkausMenossa = false;
                        btnMuokkaaValitunMokinTietoja.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Tarkasta tekstikenttien täyttö");
                    }

                }
            }
            else
            {
                MessageBox.Show("Laita ensin haku pois päältä 'Lopeta'-napista");
            }
            
        } //antaa käyttäjän muokata tietoja
        private void UpdateDatabaseAndDataGridView()
        {
            int alueid = int.Parse(cmbUusiMokkiValitseAlueID.SelectedValue.ToString());
            string postinro = cmbUusiMokkiValitsePostiNro.Text.ToString();
            string mokkinimi = tbValittuMokkiNimi.Text.ToString();
            string katuosoite = tbValittuMokkiOsoite.Text.ToString();
            double hinta = double.Parse(tbValittuMokkiHintaVrk.Text);
            string mokinkuvaus = rtbValittuMokkiKuvaus.Text.ToString();
            int hlomaara = int.Parse(tbValittuMokkiHloMaara.Text);
            string mokinvarustelu = rtbValittuMokkiVarustelu.Text.ToString();
            int mokkiid = int.Parse(dgMokkiLista.CurrentRow.Cells[0].Value.ToString());

            string updateQuery = "UPDATE mokki SET alue_id=@alueid, postinro=@postinro, mokkinimi=@mokkinimi, katuosoite=@katuosoite, hinta=@hinta, kuvaus=@mokinkuvaus, henkilomaara=@hlomaara, varustelu=@mokinvarustelu WHERE mokki_id=@mokkiid";

            using (connection)
            {
                using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@mokkiid", mokkiid);
                    command.Parameters.AddWithValue("@alueid", alueid);
                    command.Parameters.AddWithValue("@postinro", postinro);
                    command.Parameters.AddWithValue("@mokkinimi", mokkinimi);
                    command.Parameters.AddWithValue("@katuosoite", katuosoite);
                    command.Parameters.AddWithValue("@hinta", hinta);
                    command.Parameters.AddWithValue("@mokinkuvaus", mokinkuvaus);
                    command.Parameters.AddWithValue("@hlomaara", hlomaara);
                    command.Parameters.AddWithValue("@mokinvarustelu", mokinvarustelu);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message + "\n\n" + ex);
                    }

                }
            }
            UpdatedgMokkiLista();
        } //päivittää tietokantaa, jos käyttäjä on halunnut muokata mökin tietoja
        private bool ValidateTexts()
        {
            int alueid;
            double hinta;
            int hlomaara;

            if (!int.TryParse(tbValittuMokkiHloMaara.Text, out hlomaara))
            {
                return false;
            }

            // tarkastaa double-tekstikentät
            if (!double.TryParse(tbValittuMokkiHintaVrk.Text, out hinta))
            {
                return false;
            }

            // tarkastaa string-tekstikentät
            if (string.IsNullOrWhiteSpace(cmbUusiMokkiValitsePostiNro.Text) || !Regex.IsMatch(cmbUusiMokkiValitsePostiNro.Text, @"^\d{5}$") ||
                string.IsNullOrWhiteSpace(tbValittuMokkiNimi.Text) ||
                string.IsNullOrWhiteSpace(tbValittuMokkiOsoite.Text) ||
                string.IsNullOrWhiteSpace(rtbValittuMokkiKuvaus.Text) ||
                string.IsNullOrWhiteSpace(rtbValittuMokkiVarustelu.Text))
            {
                return false;
            }

            return true;
        } // tarkastaa tekstikenttien oikeanmallisen täytön

        private void btnHaeMokit_Click(object sender, EventArgs e)
        {
            if(btnHaeMokit.Text == "Rajaa hakua")
            {
                dgMokkiLista.ClearSelection();
                hakuPaalla = true;
                btnHaeMokit.Text = "Lopeta";
                cmbUusiMokkiValitseAlueID.Enabled = false;
                cmbUusiMokkiValitsePostiNro.Enabled = false;
                tbValittuMokkiNimi.Visible = false;
                tbValittuMokkiOsoite.Visible = false;
                tbValittuMokkiHintaVrk.Visible = false;
                rtbValittuMokkiKuvaus.Visible = false;
                tbValittuMokkiHloMaara.Visible = false;
                rtbValittuMokkiVarustelu.Visible = false;
                btnSuoritaMokkienHaku.Visible = true;
                btnSuoritaMokkienHaku.Enabled = true;
                checkAlueID.Visible = true;
                checkPostiNro.Visible = true;
                lbl2.Visible = false;
                lbl3.Visible = false;
                lbl4.Visible = false;
                lbl5.Visible = false;
                lbl6.Visible = false;
                lbl7.Visible = false;
                lbl8.Visible = false;
                lbl9.Visible = false;
                btnLisaaMokinTiedot.Visible = false;
                btnMuokkaaValitunMokinTietoja.Visible = false;
                lblHaeAlueID.Visible = true;
                lblHaePostiNro.Visible = true;
                lblHakuOhjeet.Location = new Point(7, 127);
                lblHakuOhjeet.Visible = true;

            }
            else
            {
                UpdatedgMokkiLista();
                hakuPaalla = false;
                btnHaeMokit.Text = "Rajaa hakua";
                tbValittuMokkiNimi.Visible = true;
                tbValittuMokkiOsoite.Visible = true;
                tbValittuMokkiHintaVrk.Visible = true;
                rtbValittuMokkiKuvaus.Visible = true;
                tbValittuMokkiHloMaara.Visible = true;
                rtbValittuMokkiVarustelu.Visible = true;
                btnSuoritaMokkienHaku.Visible = false;
                btnSuoritaMokkienHaku.Enabled = false;
                checkAlueID.Visible = false;
                checkPostiNro.Visible = false;
                lbl2.Visible = true;
                lbl3.Visible = true;
                lbl4.Visible = true;
                lbl5.Visible = true;
                lbl6.Visible = true;
                lbl7.Visible = true;
                lbl8.Visible = true;
                lbl9.Visible = true;
                btnLisaaMokinTiedot.Visible = true;
                btnMuokkaaValitunMokinTietoja.Visible = true;
                lblHaeAlueID.Visible = false;
                lblHaePostiNro.Visible = false;
                lblHakuOhjeet.Location = new Point(265, 156);
                lblHakuOhjeet.Visible = false;
                cmbUusiMokkiValitseAlueID.Enabled = true;
                cmbUusiMokkiValitsePostiNro.Enabled = true;
            }
        } // laittaa hakemistilan päälle

        private void btnSuoritaMokkienHaku_Click(object sender, EventArgs e)
        {
            hakuPaalla = false;
            ClearTextBoxes();
            btnHaeMokit.Text = "Rajaa hakua";
            tbValittuMokkiNimi.Visible = true;
            tbValittuMokkiOsoite.Visible = true;
            tbValittuMokkiHintaVrk.Visible = true;
            rtbValittuMokkiKuvaus.Visible = true;
            tbValittuMokkiHloMaara.Visible = true;
            rtbValittuMokkiVarustelu.Visible = true;
            btnSuoritaMokkienHaku.Visible = false;
            btnSuoritaMokkienHaku.Enabled = false;
            checkAlueID.Visible = false;
            checkPostiNro.Visible = false;
            lbl2.Visible = true;
            lbl3.Visible = true;
            lbl4.Visible = true;
            lbl5.Visible = true;
            lbl6.Visible = true;
            lbl7.Visible = true;
            lbl8.Visible = true;
            lbl9.Visible = true;
            btnLisaaMokinTiedot.Visible = true;
            btnMuokkaaValitunMokinTietoja.Visible = true;
            lblHaeAlueID.Visible = false;
            lblHaePostiNro.Visible = false;
            lblHakuOhjeet.Location = new Point(265, 156);
            lblHakuOhjeet.Visible = false;
            try
            {
                if (cmbUusiMokkiValitseAlueID.Enabled && cmbUusiMokkiValitsePostiNro.Enabled)
                {
                    int alueid = Convert.ToInt32(cmbUusiMokkiValitseAlueID.SelectedValue);
                    try
                    {
                        string postinro = cmbUusiMokkiValitsePostiNro.SelectedValue.ToString();
                        string hakuQuery = "SELECT * FROM mokki WHERE alue_id = @alueid AND postinro = @postinro";
                        DataTable datatable = new DataTable();
                        using (connection)
                        {
                            MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                            command.Parameters.AddWithValue("@alueid", alueid);
                            command.Parameters.AddWithValue("@postinro", postinro);
                            MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                            adapter.Fill(datatable);
                            connection.Close();
                        }
                        dgMokkiLista.DataSource = datatable;


                        checkAlueID.Checked = false;
                        checkPostiNro.Checked = false;
                    }
                    catch
                    {
                        MessageBox.Show("Haku ei onnistunut");
                    }

                    
                }
                else if (cmbUusiMokkiValitseAlueID.Enabled && !cmbUusiMokkiValitsePostiNro.Enabled)
                {
                    int alueid = Convert.ToInt32(cmbUusiMokkiValitseAlueID.SelectedValue);

                    string hakuQuery = "SELECT * FROM mokki WHERE alue_id = @alueid";
                    DataTable datatable = new DataTable();
                    using (connection)
                    {
                        MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                        command.Parameters.AddWithValue("@alueid", alueid);
                        MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                        adapter.Fill(datatable);
                        connection.Close();
                    }
                    dgMokkiLista.DataSource = datatable;

                    checkAlueID.Checked = false;
                    checkPostiNro.Checked = false;
                }
                else if (!cmbUusiMokkiValitseAlueID.Enabled && cmbUusiMokkiValitsePostiNro.Enabled)
                {
                    string postinro = cmbUusiMokkiValitsePostiNro.SelectedValue.ToString();

                    string hakuQuery = "SELECT * FROM mokki WHERE postinro = @postinro";
                    DataTable datatable = new DataTable();
                    using (connection)
                    {
                        MySqlCommand command = new MySqlCommand(hakuQuery, connection);
                        command.Parameters.AddWithValue("@postinro", postinro);
                        MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                        adapter.Fill(datatable);
                        connection.Close();
                    }
                    dgMokkiLista.DataSource = datatable;

                    checkAlueID.Checked = false;
                    checkPostiNro.Checked = false;
                }
                else
                {
                    UpdatedgMokkiLista();
                    checkAlueID.Checked = false;
                    checkPostiNro.Checked = false;
                }
                cmbUusiMokkiValitseAlueID.Enabled = true;
                cmbUusiMokkiValitsePostiNro.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Haku ei onnistunut");
            }
                
        } // hakee mökkejä, jotka sisältävät valitut hakuehdot

        private void checkAlueID_CheckedChanged(object sender, EventArgs e)
        {
            if (!cmbUusiMokkiValitseAlueID.Enabled)
            {
                cmbUusiMokkiValitseAlueID.Enabled = true;
            }
            else
            {
                cmbUusiMokkiValitseAlueID.Enabled = false;
            }
        }   //alue_id lähtee pois/tulee hakukriteereihin

        private void checkPostiNro_CheckedChanged(object sender, EventArgs e)
        {
            if (!cmbUusiMokkiValitsePostiNro.Enabled)
            {
                cmbUusiMokkiValitsePostiNro.Enabled = true;
            }
            else
            {
                cmbUusiMokkiValitsePostiNro.Enabled = false;
            }
        } //postinumero lähtee pois/tulee hakukriteereihin

        private void btnAlueHallintaan_Click(object sender, EventArgs e)
        {
            var toiminta= new FormToiminta();
            this.Hide();
            toiminta.ShowDialog();
            this.Close();

        } // avaa Toiminta-alueiden hallintaformin, jotta käyttäjä pääsee lisäämään uusia toimialueita ja postitoimipaikkoja järjestelmään
    }
}
