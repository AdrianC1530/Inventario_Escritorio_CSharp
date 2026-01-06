using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace Proyecto5denuevo
{
    public partial class Form1 : Form
    {
        private List<Producto> inventario = new List<Producto>();
        string rutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InventarioZonnTech.xlsx");

        public Form1()
        {
            InitializeComponent();

            
            this.button2.Click -= new EventHandler(this.button2_Click);
            this.button2.Click += new EventHandler(this.button2_Click);

            this.button3.Click -= new EventHandler(this.button3_Click);
            this.button3.Click += new EventHandler(this.button3_Click);

            this.btnRealizarAccion.Click -= new EventHandler(this.button1_Click);
            this.btnRealizarAccion.Click += new EventHandler(this.button1_Click);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView1.ReadOnly = true;
            }
            catch { }

            panel2.Visible = false;
            VerificarYCrearExcel();
            CargarDesdeExcel();
        }

        // ---------------------------------------------------------
        // GESTIÓN EXCEL
        // ---------------------------------------------------------
        private void VerificarYCrearExcel()
        {
            if (!File.Exists(rutaArchivo))
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inventario");
                    worksheet.Cell(1, 1).Value = "Nombre";
                    worksheet.Cell(1, 2).Value = "Marca";
                    worksheet.Cell(1, 3).Value = "Precio";
                    worksheet.Cell(1, 4).Value = "Cantidad";
                    worksheet.Cell(1, 5).Value = "Vendidos";
                    workbook.SaveAs(rutaArchivo);
                }
            }
        }

        private void CargarDesdeExcel()
        {
            try
            {
                inventario.Clear();
                if (File.Exists(rutaArchivo))
                {
                    using (var workbook = new XLWorkbook(rutaArchivo))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var filas = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var fila in filas)
                        {
                            inventario.Add(new Producto
                            {
                                Nombre = fila.Cell(1).GetValue<string>(),
                                Marca = fila.Cell(2).GetValue<string>(),
                                Precio = fila.Cell(3).GetValue<decimal>(),
                                Cantidad = fila.Cell(4).GetValue<int>(),
                                Vendidos = fila.Cell(5).GetValue<int>()
                            });
                        }
                    }
                }
                ActualizarGrid(inventario);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al leer Excel: " + ex.Message);
            }
        }

        private void GuardarEnExcel()
        {
            try
            {
                if (File.Exists(rutaArchivo)) File.Delete(rutaArchivo);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inventario");
                    worksheet.Cell(1, 1).Value = "Nombre";
                    worksheet.Cell(1, 2).Value = "Marca";
                    worksheet.Cell(1, 3).Value = "Precio";
                    worksheet.Cell(1, 4).Value = "Cantidad";
                    worksheet.Cell(1, 5).Value = "Vendidos";

                    for (int i = 0; i < inventario.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = inventario[i].Nombre;
                        worksheet.Cell(i + 2, 2).Value = inventario[i].Marca;
                        worksheet.Cell(i + 2, 3).Value = inventario[i].Precio;
                        worksheet.Cell(i + 2, 4).Value = inventario[i].Cantidad;
                        worksheet.Cell(i + 2, 5).Value = inventario[i].Vendidos;
                    }
                    workbook.SaveAs(rutaArchivo);
                }
            }
            catch (Exception ex) { MessageBox.Show("Error guardando Excel: " + ex.Message); }
        }

        private void ActualizarGrid(List<Producto> lista)
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = lista;
        }

        // ---------------------------------------------------------
        // LÓGICA DE BOTONES (MENU)
        // ---------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            if (rbtMostrarInventario.Checked)
            {
                CargarDesdeExcel();
                panel2.Visible = false;
            }
            else if (rbtMostrarVentas.Checked)
            {
                var listaTop = inventario.OrderByDescending(p => p.Vendidos).ToList();
                ActualizarGrid(listaTop);
                panel2.Visible = false;
                MessageBox.Show("Informe: Productos ordenados por mayor venta.");
            }
            else if (rbtElementos.Checked)
            {
                panel2.Visible = true;
                LimpiarCajas();
                textBox1.Enabled = true; textBox2.Enabled = true; textBox3.Enabled = true;
                textBox4.Enabled = true;
                button2.Text = "Agregar Elemento";
            }
            else if (rbtModificar.Checked)
            {
                panel2.Visible = true;
                LimpiarCajas();
                MessageBox.Show("MODIFICAR: Ingrese NOMBRE. Puede editar Precio y Cantidad.");
                textBox1.Enabled = true; textBox2.Enabled = false; textBox3.Enabled = true;
                textBox4.Enabled = true;
                button2.Text = "Actualizar Stock";
            }
            // --- NUEVA OPCIÓN: VENDER (radioButton2) ---
            else if (radioButton2.Checked)
            {
                panel2.Visible = true;
                LimpiarCajas();
                MessageBox.Show("VENTA: Ingrese el NOMBRE del producto y la CANTIDAD a vender.");

                textBox1.Enabled = true;  // Nombre (Necesario para buscar)
                textBox2.Enabled = false; // Marca (No necesaria para vender)
                textBox3.Enabled = false; // Precio (No necesario para vender)
                textBox4.Enabled = true;  // Cantidad (Lo que se va a vender)

                button2.Text = "Confirmar Venta"; // Cambiamos el botón para que haga la venta
            }
            else if (radioButton1.Checked) // Agotados
            {
                var agotados = inventario.Where(p => p.Cantidad == 0).ToList();
                ActualizarGrid(agotados);
                panel2.Visible = false;
                MessageBox.Show("Mostrando productos agotados.");
            }
        }

        // ---------------------------------------------------------
        // LÓGICA DE AGREGAR / ACTUALIZAR / VENDER (BOTÓN 2)
        // ---------------------------------------------------------
        private void button2_Click(object sender, EventArgs e)
        {
            // --- LÓGICA DE VENTA ---
            if (button2.Text == "Confirmar Venta")
            {
                string nombre = textBox1.Text;
                var producto = inventario.FirstOrDefault(p => p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

                if (producto != null)
                {
                    if (int.TryParse(textBox4.Text, out int cantidadVenta) && cantidadVenta > 0)
                    {
                        if (producto.Cantidad >= cantidadVenta)
                        {
                            // Realizar la venta
                            producto.Cantidad = producto.Cantidad - cantidadVenta; // Restar del inventario
                            producto.Vendidos = producto.Vendidos + cantidadVenta; // Sumar al historial de ventas

                            GuardarEnExcel();
                            ActualizarGrid(inventario);
                            MessageBox.Show($"¡Venta realizada! Se vendieron {cantidadVenta} unidades de {producto.Nombre}.");
                            panel2.Visible = false;
                            LimpiarCajas();
                        }
                        else
                        {
                            MessageBox.Show($"Error: Stock insuficiente. Solo quedan {producto.Cantidad} unidades.");
                        }
                    }
                    else MessageBox.Show("Error: Ingrese una cantidad válida mayor a 0.");
                }
                else MessageBox.Show("Error: Producto no encontrado. Verifique el nombre.");
            }
            // --- LÓGICA DE ACTUALIZAR ---
            else if (button2.Text == "Actualizar Stock")
            {
                string nombre = textBox1.Text;
                var producto = inventario.FirstOrDefault(p => p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

                if (producto != null)
                {
                    bool valido = true;
                    if (int.TryParse(textBox4.Text, out int nuevaCant)) producto.Cantidad = nuevaCant;
                    else if (!string.IsNullOrEmpty(textBox4.Text)) valido = false;

                    if (decimal.TryParse(textBox3.Text, out decimal nuevoPrecio)) producto.Precio = nuevoPrecio;
                    else if (!string.IsNullOrEmpty(textBox3.Text)) valido = false;

                    if (valido)
                    {
                        GuardarEnExcel();
                        ActualizarGrid(inventario);
                        MessageBox.Show("Producto actualizado.");
                        panel2.Visible = false;
                        LimpiarCajas();
                    }
                    else MessageBox.Show("Datos inválidos.");
                }
                else MessageBox.Show("Producto no encontrado.");
            }
            // --- LÓGICA DE AGREGAR NUEVO ---
            else
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(textBox1.Text)) { MessageBox.Show("Falta Nombre."); return; }

                    Producto p = new Producto();
                    p.Nombre = textBox1.Text;
                    p.Marca = textBox2.Text;
                    p.Precio = decimal.Parse(textBox3.Text);
                    p.Cantidad = int.Parse(textBox4.Text);
                    p.Vendidos = 0;

                    inventario.Add(p);
                    GuardarEnExcel();
                    ActualizarGrid(inventario);

                    MessageBox.Show("Producto guardado.");
                    panel2.Visible = false;
                    LimpiarCajas();
                }
                catch (Exception) { MessageBox.Show("Revise que Precio y Cantidad sean números."); }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            LimpiarCajas();
        }

        private void LimpiarCajas()
        {
            textBox1.Clear(); textBox2.Clear(); textBox3.Clear(); textBox4.Clear();
        }

        private void label1_Click(object sender, EventArgs e) { }
    }

    public class Producto
    {
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public int Vendidos { get; set; }
    }
}