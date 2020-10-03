using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimioAPI;
using SimioAPI.Extensions;
using SimioAPI.Graphics;
using Simio;
using Simio.SimioEnums;



namespace _MYS1_Practica3_P26
{
    public partial class Form1 : Form
    {
        static ISimioProject _ProyectoSimio;
        String _rutaproyecto = "[MYS1]ModeloBase_P26.spfx";
        int ContadorPath = 1, ContadorServer = 1, ContadorSeparador = 1, ContadorSource = 1, ContadorSink = 1, ContadorPathSimple = 1;
        String[] warnings;
        IModel model;
        IIntelligentObjects intelligentObjects;

        public Form1()
        {
            _ProyectoSimio = SimioProjectFactory.LoadProject(_rutaproyecto, out warnings);
            model = _ProyectoSimio.Models["Model"];
            //model = _ProyectoSimio.Models[1];
            intelligentObjects = model.Facility.IntelligentObjects;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //Source 
                intelligentObjects.CreateObject("Source", new FacilityLocation(5, 0, -5));
                //Cambiar el interarrival time
                model.Facility.IntelligentObjects["Source1"].Properties["InterarrivalTime"].Value = "1";
                //Server
                intelligentObjects.CreateObject("Server", new FacilityLocation(10, 0, 5));
                //Cambiar processing time
                model.Facility.IntelligentObjects["Server1"].Properties["ProcessingTime"].Value = "2";
                //Sink
                intelligentObjects.CreateObject("Sink", new FacilityLocation(15, 0, -5));
                //Source a server
                intelligentObjects.CreateLink("Path", ((IFixedObject)model.Facility.IntelligentObjects["Source1"]).Nodes[0], ((IFixedObject)model.Facility.IntelligentObjects["Server1"]).Nodes[0], null);
                //Server a sink
                intelligentObjects.CreateLink("Path", ((IFixedObject)model.Facility.IntelligentObjects["Server1"]).Nodes[1], ((IFixedObject)model.Facility.IntelligentObjects["Sink1"]).Nodes[0], null);
                //Incrementar contadores
                ContadorSource++;
                ContadorServer++;
                ContadorSink++;
                ContadorPathSimple++;
                ContadorPathSimple++;
                try
                {
                    SimioProjectFactory.SaveProject(_ProyectoSimio, "ModeloModificado.spfx", out warnings);
                }
                catch (Exception er)
                {
                    MessageBox.Show("Error: " + er.Message);
                }
                MessageBox.Show("Finalizo guardado");
                //Console.WriteLine("Finalizo guardado");
            }
            catch (ArgumentOutOfRangeException outOfRange)
            {
                MessageBox.Show("Error al crear " + outOfRange.Message);
                //Console.WriteLine("Error al crear elementos");
            }
        }
    }
}
