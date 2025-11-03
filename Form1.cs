using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ProyectoParcial1
{
    public partial class Form1 : Form
    {
        // Cadena de conexión
        string connectionString = "Server=localhost;Database=BibliotecaDB;Uid=root;Pwd=12345;";
        MySqlConnection conexion;

        public Form1()
        {
            InitializeComponent();
            conexion = new MySqlConnection(connectionString);
            lblStatus.Text = "Sistema listo. Conectado a MySQL.";

            // Vincular eventos de controles
            listBoxResultados.SelectedIndexChanged += listBoxResultados_SelectedIndexChanged;
            textBoxTitulo.TextChanged += textBoxTitulo_TextChanged;
            textBoxAutor.TextChanged += textBoxAutor_TextChanged;
            textBoxISBN.TextChanged += textBoxISBN_TextChanged;
            textBoxGenero.TextChanged += textBoxGenero_TextChanged;
            textBoxAnio.TextChanged += textBoxAnio_TextChanged;
            lblStatus.Click += lblStatus_Click;

            MostrarTodosEnListBoxBD();
        }

        #region Validaciones
        private bool EsISBNValido(string isbn)
        {
            return !string.IsNullOrWhiteSpace(isbn);
        }

        private bool EsAnioValido(string anioTxt, out int anio)
        {
            if (int.TryParse(anioTxt, out anio))
            {
                if (anio > 0 && anio <= DateTime.Now.Year) return true;
            }
            anio = 0;
            return false;
        }
        #endregion

        #region Operaciones BD
        private void AltaLibroEnBD(string titulo, string autor, string isbn, string genero, int anio)
        {
            try
            {
                conexion.Open();
                string checkSql = "SELECT COUNT(*) FROM Libro WHERE ISBN=@ISBN";
                MySqlCommand checkCmd = new MySqlCommand(checkSql, conexion);
                checkCmd.Parameters.AddWithValue("@ISBN", isbn);
                long existe = (long)checkCmd.ExecuteScalar();
                if (existe > 0)
                {
                    lblStatus.Text = "Error: Ya existe un libro con ese ISBN.";
                    return;
                }

                string sql = "INSERT INTO Libro (Titulo, Autor, ISBN, Genero, Anio, Activo) VALUES (@Titulo, @Autor, @ISBN, @Genero, @Anio, 1)";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@Titulo", titulo);
                cmd.Parameters.AddWithValue("@Autor", autor);
                cmd.Parameters.AddWithValue("@ISBN", isbn);
                cmd.Parameters.AddWithValue("@Genero", genero);
                cmd.Parameters.AddWithValue("@Anio", anio);
                cmd.ExecuteNonQuery();

                lblStatus.Text = "Alta exitosa.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                conexion.Close();
            }
        }

        private void BajaLibroEnBD(string isbn)
        {
            try
            {
                conexion.Open();
                string sql = "DELETE FROM Libro WHERE ISBN=@ISBN";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@ISBN", isbn);
                int filas = cmd.ExecuteNonQuery();
                lblStatus.Text = filas > 0 ? "Baja realizada." : "No se encontró ISBN.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                conexion.Close();
            }
        }

        private void CambioLibroEnBD(string isbn, string nuevoTitulo, string nuevoAutor, string nuevoGenero, string anioTxt)
        {
            try
            {
                conexion.Open();
                string sqlCheck = "SELECT COUNT(*) FROM Libro WHERE ISBN=@ISBN";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conexion);
                cmdCheck.Parameters.AddWithValue("@ISBN", isbn);
                long existe = (long)cmdCheck.ExecuteScalar();
                if (existe == 0)
                {
                    lblStatus.Text = "Error: No existe libro con ese ISBN.";
                    return;
                }

                string sql = "UPDATE Libro SET Titulo=@Titulo, Autor=@Autor, Genero=@Genero, Anio=@Anio WHERE ISBN=@ISBN";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@ISBN", isbn);

                cmd.Parameters.AddWithValue("@Titulo", string.IsNullOrWhiteSpace(nuevoTitulo) ? ObtenerCampoBD(isbn, "Titulo") : nuevoTitulo);
                cmd.Parameters.AddWithValue("@Autor", string.IsNullOrWhiteSpace(nuevoAutor) ? ObtenerCampoBD(isbn, "Autor") : nuevoAutor);
                cmd.Parameters.AddWithValue("@Genero", string.IsNullOrWhiteSpace(nuevoGenero) ? ObtenerCampoBD(isbn, "Genero") : nuevoGenero);

                if (!string.IsNullOrWhiteSpace(anioTxt) && EsAnioValido(anioTxt, out int anio))
                    cmd.Parameters.AddWithValue("@Anio", anio);
                else
                    cmd.Parameters.AddWithValue("@Anio", ObtenerCampoBD(isbn, "Anio"));

                cmd.ExecuteNonQuery();
                lblStatus.Text = "Cambio realizado.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                conexion.Close();
            }
        }

        private string ObtenerCampoBD(string isbn, string campo)
        {
            try
            {
                string valor = "";
                conexion.Open();
                string sql = $"SELECT {campo} FROM Libro WHERE ISBN=@ISBN";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@ISBN", isbn);
                var result = cmd.ExecuteScalar();
                if (result != null) valor = result.ToString();
                return valor;
            }
            finally
            {
                conexion.Close();
            }
        }

        private void MostrarTodosEnListBoxBD()
        {
            try
            {
                listBoxResultados.Items.Clear();
                conexion.Open();
                string sql = "SELECT * FROM Libro WHERE Activo=1";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string linea = $"{reader["Titulo"]} | {reader["Autor"]} | ISBN:{reader["ISBN"]} | {reader["Genero"]} | {reader["Anio"]}";
                    listBoxResultados.Items.Add(linea);
                }
                if (listBoxResultados.Items.Count == 0)
                    listBoxResultados.Items.Add("No hay registros.");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                conexion.Close();
            }
        }

        private void MostrarPorAutorBD(string autorBuscar)
        {
            try
            {
                listBoxResultados.Items.Clear();
                conexion.Open();
                string sql = "SELECT * FROM Libro WHERE Activo=1 AND Autor LIKE @Autor";
                MySqlCommand cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@Autor", "%" + autorBuscar + "%");
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string linea = $"{reader["Titulo"]} | {reader["Autor"]} | ISBN:{reader["ISBN"]} | {reader["Genero"]} | {reader["Anio"]}";
                    listBoxResultados.Items.Add(linea);
                }
                if (listBoxResultados.Items.Count == 0)
                    listBoxResultados.Items.Add("No se encontraron libros para ese autor.");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                conexion.Close();
            }
        }
        #endregion

        #region Eventos Botones
        private void btnAlta_Click(object sender, EventArgs e)
        {
            string titulo = textBoxTitulo.Text.Trim();
            string autor = textBoxAutor.Text.Trim();
            string isbn = textBoxISBN.Text.Trim();
            string genero = textBoxGenero.Text.Trim();
            string anioTxt = textBoxAnio.Text.Trim();

            if (string.IsNullOrWhiteSpace(titulo) ||
                string.IsNullOrWhiteSpace(autor) ||
                string.IsNullOrWhiteSpace(isbn))
            {
                lblStatus.Text = "Error: Título, Autor y ISBN son obligatorios.";
                return;
            }

            if (!EsISBNValido(isbn))
            {
                lblStatus.Text = "Error: ISBN inválido.";
                return;
            }

            if (!EsAnioValido(anioTxt, out int anio))
            {
                lblStatus.Text = "Error: Año inválido.";
                return;
            }

            AltaLibroEnBD(titulo, autor, isbn, genero, anio);
            LimpiarCamposDatos();
            MostrarTodosEnListBoxBD();
        }

        private void btnBaja_Click(object sender, EventArgs e)
        {
            string isbn = textBoxISBN.Text.Trim();
            if (!EsISBNValido(isbn))
            {
                lblStatus.Text = "Error: Ingresa ISBN para baja.";
                return;
            }

            var resp = MessageBox.Show($"Eliminar libro con ISBN: {isbn}?", "Confirmar baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resp != DialogResult.Yes)
            {
                lblStatus.Text = "Baja cancelada.";
                return;
            }

            BajaLibroEnBD(isbn);
            MostrarTodosEnListBoxBD();
        }

        private void btnCambio_Click(object sender, EventArgs e)
        {
            string isbn = textBoxISBN.Text.Trim();
            CambioLibroEnBD(isbn, textBoxTitulo.Text.Trim(), textBoxAutor.Text.Trim(), textBoxGenero.Text.Trim(), textBoxAnio.Text.Trim());
            MostrarTodosEnListBoxBD();
        }

        private void btnConsultaTodos_Click(object sender, EventArgs e)
        {
            MostrarTodosEnListBoxBD();
            lblStatus.Text = "Consulta: Todos los libros mostrados.";
        }

        private void btnConsultaAutor_Click(object sender, EventArgs e)
        {
            string autorBuscar = textBoxAutor.Text.Trim();
            if (string.IsNullOrWhiteSpace(autorBuscar))
            {
                lblStatus.Text = "Error: Ingresa autor para la búsqueda.";
                return;
            }
            MostrarPorAutorBD(autorBuscar);
            lblStatus.Text = $"Consulta por autor: '{autorBuscar}' completada.";
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            var r = MessageBox.Show("¿Deseas salir? Se perderán los datos en memoria.", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
                this.Close();
        }
        #endregion

        #region Eventos TextBox, ListBox y Label (vacíos pero necesarios)
        private void listBoxResultados_SelectedIndexChanged(object sender, EventArgs e) { }
        private void textBoxTitulo_TextChanged(object sender, EventArgs e) { }
        private void textBoxAutor_TextChanged(object sender, EventArgs e) { }
        private void textBoxISBN_TextChanged(object sender, EventArgs e) { }
        private void textBoxGenero_TextChanged(object sender, EventArgs e) { }
        private void textBoxAnio_TextChanged(object sender, EventArgs e) { }
        private void lblStatus_Click(object sender, EventArgs e) { }
        #endregion

        private void LimpiarCamposDatos()
        {
            textBoxTitulo.Text = "";
            textBoxAutor.Text = "";
            textBoxISBN.Text = "";
            textBoxGenero.Text = "";
            textBoxAnio.Text = "";
        }
    }
}
