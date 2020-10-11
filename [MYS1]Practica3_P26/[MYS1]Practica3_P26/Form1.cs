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
using System.Net.Sockets;


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
            model = _ProyectoSimio.Models[1];
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
                crearRegiones();
                crearEnlaces();
                crearAeropuertos();
                //dibujarCarnets();
                pintarMapa();
                try
                {
                    SimioProjectFactory.SaveProject(_ProyectoSimio, "ModeloModificado.spfx", out warnings);
                    System.Diagnostics.Process.Start("ModeloModificado.spfx");
                    MessageBox.Show("Finalizo guardado");
                }
                catch (Exception er)
                {
                    MessageBox.Show("Error: " + er.Message);
                }

                //Console.WriteLine("Finalizo guardado");
            }
            catch (ArgumentOutOfRangeException outOfRange)
            {
                MessageBox.Show("Error al crear " + outOfRange.Message);
                //Console.WriteLine("Error al crear elementos");
            }
        }

        public void crearRegiones() {
            crearRegion("metropolitana", 0, 0, "200", "Random.Poisson(2)", "minutos", "Random.Exponential(4)");
            crearRegion("norte", -20, 5, "50", "Random.Poisson(8)", "minutos", "Random.Exponential(5)");
            crearRegion("nororiente", -10, 30, "40", "Random.Poisson(6)", "minutos", "Random.Exponential(3)");
            crearRegion("suroriente", 20, 15, "30", "Random.Poisson(10)", "minutos", "Random.Exponential(4)");
            crearRegion("central", 20, -15, "100", "Random.Poisson(3)", "minutos", "Random.Exponential(5)");
            crearRegion("suroccidente", 10, -40, "120", "Random.Poisson(4)", "minutos", "Random.Exponential(3)");
            crearRegion("noroccidente", -20, -40, "30", "Random.Poisson(12)", "minutos", "Random.Exponential(6)");
            crearRegion("peten", -50, 10, "150", "Random.Poisson(4)", "minutos", "Random.Exponential(4)");
        }

        public void crearRegion(string nombre, long latitud_, long longitud_, string capacidad_, string tiempoLlegada_, string unidadTiempo, string tiempoAtencion_)
        {
            //Server que simula la estacion de servicio ubicada en cada region 
            IFixedObject estacion = model.Facility.IntelligentObjects.CreateObject("Server", new FacilityLocation(longitud_, 0, latitud_)) as IFixedObject;
            estacion.ObjectName = "region" + nombre.ToUpper();
            model.Facility.IntelligentObjects["region" + nombre.ToUpper()].Properties["ProcessingTime"].Value = tiempoAtencion_;
            model.Facility.IntelligentObjects["region" + nombre.ToUpper()].Properties["InputBufferCapacity"].Value = capacidad_;
            model.Facility.IntelligentObjects["output@region" + nombre.ToUpper()].Properties["OutboundLinkRule"].Value = "ByLinkWeight";
            //Source que genera turistas 
            IFixedObject turistas = model.Facility.IntelligentObjects.CreateObject("Source", new FacilityLocation(longitud_ - 6, 0, latitud_)) as IFixedObject;
            turistas.ObjectName = "turistas" + nombre.ToUpper();
            model.Facility.IntelligentObjects["turistas" + nombre.ToUpper()].Properties["InterarrivalTime"].Value = tiempoLlegada_;
            //Nodo
            INodeObject union = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(longitud_ - 3, 0, latitud_)) as INodeObject;
            union.ObjectName = "union" + nombre.ToUpper();
            //Nodo Regreso 
            INodeObject regreso = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(longitud_ - 3, 0, latitud_ - 3)) as INodeObject;
            regreso.ObjectName = "regreso" + nombre.ToUpper();
            regreso.Properties["OutboundLinkRule"].Value = "ByLinkWeight";
            //Enlace entre TransferNode y node 
            ILinkObject path1 = model.Facility.IntelligentObjects.CreateLink("Path", regreso, union, null) as ILinkObject;
            path1.ObjectName = "path1" + nombre.ToUpper();
            //Enlace entre source y node 
            ILinkObject path2 = model.Facility.IntelligentObjects.CreateLink("Path", turistas.Nodes[0], union, null) as ILinkObject;
            path2.ObjectName = "path2" + nombre.ToUpper();
            //Enlace entre node y server 
            ILinkObject path3 = model.Facility.IntelligentObjects.CreateLink("Path", union, estacion.Nodes[0], null) as ILinkObject;
            path3.ObjectName = "path3" + nombre.ToUpper();
        }

        public void crearEnlaces() {
            crearEnlace(1, 1, "0.35", "0");
            crearEnlace(1, 5, "0.30", "63000");
            crearEnlace(1, 4, "0.15", "124000");
            crearEnlace(1, 3, "0.20", "241000");

            crearEnlace(2, 2, "0.40", "0");
            crearEnlace(2, 8, "0.40", "147000");
            crearEnlace(2, 3, "0.10", "138000");
            crearEnlace(2, 7, "0.10", "145000");

            crearEnlace(3, 3, "0.20", "0");
            crearEnlace(3, 1, "0.30", "241000");
            crearEnlace(3, 2, "0.15", "138000");
            crearEnlace(3, 4, "0.05", "231000");
            crearEnlace(3, 8, "0.30", "282000");

            crearEnlace(4, 4, "0.40", "0");
            crearEnlace(4, 3, "0.20", "231000");
            crearEnlace(4, 1, "0.25", "124000");
            crearEnlace(4, 5, "0.15", "154000");

            crearEnlace(5, 5, "0.35", "0");
            crearEnlace(5, 1, "0.35", "63000");
            crearEnlace(5, 4, "0.05", "154000");
            crearEnlace(5, 6, "0.15", "155000");
            crearEnlace(5, 7, "0.10", "269000");

            crearEnlace(6, 6, "0.35", "0");
            crearEnlace(6, 7, "0.30", "87000");
            crearEnlace(6, 5, "0.35", "155000");

            crearEnlace(7, 7, "0.40", "0");
            crearEnlace(7, 6, "0.30", "87000");
            crearEnlace(7, 5, "0.10", "269000");
            crearEnlace(7, 2, "0.20", "145000");

            crearEnlace(8, 8, "0.5", "0");
            crearEnlace(8, 2, "0.25", "147000");
            crearEnlace(8, 3, "0.25", "282000");
        }

        public void crearEnlace(int origen, int destino, string probabilidad, string distancia)
        {
            string cadenaOrigen = "output@region", cadenaDestino = "regreso", nombreEnlace = "";
            switch (origen)
            {
                case 1://metropolitana
                    cadenaOrigen = cadenaOrigen + "METROPOLITANA";
                    nombreEnlace = nombreEnlace + "METROPOLITANA";
                    break;
                case 2://norte
                    cadenaOrigen = cadenaOrigen + "NORTE";
                    nombreEnlace = nombreEnlace + "NORTE";
                    break;
                case 3://nororiente
                    cadenaOrigen = cadenaOrigen + "NORORIENTE";
                    nombreEnlace = nombreEnlace + "NORORIENTE";
                    break;
                case 4://suroriente
                    cadenaOrigen = cadenaOrigen + "SURORIENTE";
                    nombreEnlace = nombreEnlace + "SURORIENTE";
                    break;
                case 5://central
                    cadenaOrigen = cadenaOrigen + "CENTRAL";
                    nombreEnlace = nombreEnlace + "CENTRAL";
                    break;
                case 6://suroccidente
                    cadenaOrigen = cadenaOrigen + "SUROCCIDENTE";
                    nombreEnlace = nombreEnlace + "SUROCCIDENTE";
                    break;
                case 7://noroccidente
                    cadenaOrigen = cadenaOrigen + "NOROCCIDENTE";
                    nombreEnlace = nombreEnlace + "NOROCCIDENTE";
                    break;
                case 8://peten
                    cadenaOrigen = cadenaOrigen + "PETEN";
                    nombreEnlace = nombreEnlace + "PETEN";
                    break;
            }

            nombreEnlace = nombreEnlace + "a";
            switch (destino)
            {
                case 1://metropolitana
                    cadenaDestino = cadenaDestino + "METROPOLITANA";
                    nombreEnlace = nombreEnlace + "METROPOLITANA";
                    break;
                case 2://norte
                    cadenaDestino = cadenaDestino + "NORTE";
                    nombreEnlace = nombreEnlace + "NORTE";
                    break;
                case 3://nororiente
                    cadenaDestino = cadenaDestino + "NORORIENTE";
                    nombreEnlace = nombreEnlace + "NORORIENTE";
                    break;
                case 4://suroriente
                    cadenaDestino = cadenaDestino + "SURORIENTE";
                    nombreEnlace = nombreEnlace + "SURORIENTE";
                    break;
                case 5://central
                    cadenaDestino = cadenaDestino + "CENTRAL";
                    nombreEnlace = nombreEnlace + "CENTRAL";
                    break;
                case 6://suroccidente
                    cadenaDestino = cadenaDestino + "SUROCCIDENTE";
                    nombreEnlace = nombreEnlace + "SUROCCIDENTE";
                    break;
                case 7://noroccidente
                    cadenaDestino = cadenaDestino + "NOROCCIDENTE";
                    nombreEnlace = nombreEnlace + "NOROCCIDENTE";
                    break;
                case 8://peten
                    cadenaDestino = cadenaDestino + "PETEN";
                    nombreEnlace = nombreEnlace + "PETEN";
                    break;
            }
            ILinkObject camino = model.Facility.IntelligentObjects.CreateLink("Conveyor", (INodeObject)model.Facility.IntelligentObjects[cadenaOrigen], (INodeObject)model.Facility.IntelligentObjects[cadenaDestino], null) as ILinkObject;
            camino.ObjectName = nombreEnlace;
            camino.Properties["DrawnToScale"].Value = "False";
            camino.Properties["LogicalLength"].Value = distancia;
            camino.Properties["SelectionWeight"].Value = probabilidad;
            camino.Properties["InitialDesiredSpeed"].Value = "19.4444444444";
        }

        public void crearAeropuertos()
        {
            crearAeropuerto(1, "70", "Math.Round(Random.Exponential(35))", "0.50", "0.50");
            crearAeropuerto(8, "40", "Math.Round(Random.Exponential(50))", "0.30", "0.70");
            crearAeropuerto(7, "30", "Math.Round(Random.Exponential(70))", "0.40", "0.60");
        }

        public void crearAeropuerto(int region, string cantidadLlegada, string tiempoLlegada, string probMarcharse, string probQuedarse)
        {
            int longitud_ = 0;
            int latitud_ = 0;
            string nombreEntrada = "ae", nombreSalida = "as", union = "union", regreso = "regreso", path = "path1";

            switch (region)
            {
                case 1://metropolitana
                    nombreEntrada = nombreEntrada + "METROPOLITANA";
                    nombreSalida = nombreSalida + "METROPOLITANA";
                    union = union + "METROPOLITANA";
                    regreso = regreso + "METROPOLITANA";
                    path = path + "METROPOLITANA";
                    break;
                case 2://norte
                    nombreEntrada = nombreEntrada + "NORTE";
                    nombreSalida = nombreSalida + "NORTE";
                    union = union + "NORTE";
                    regreso = regreso + "NORTE";
                    path = path + "NORTE";
                    longitud_ = 5;
                    latitud_ = -20;
                    break;
                case 3://nororiente
                    nombreEntrada = nombreEntrada + "NORORIENTE";
                    nombreSalida = nombreSalida + "NORORIENTE";
                    union = union + "NORORIENTE";
                    regreso = regreso + "NORORIENTE";
                    path = path + "NORORIENTE";
                    longitud_ = 30;
                    latitud_ = -10;
                    break;
                case 4://suroriente
                    nombreEntrada = nombreEntrada + "SURORIENTE";
                    nombreSalida = nombreSalida + "SURORIENTE";
                    union = union + "SURORIENTE";
                    regreso = regreso + "SURORIENTE";
                    path = path + "SURORIENTE";
                    longitud_ = 15;
                    latitud_ = 20;
                    break;
                case 5://central
                    nombreEntrada = nombreEntrada + "CENTRAL";
                    nombreSalida = nombreSalida + "CENTRAL";
                    union = union + "CENTRAL";
                    regreso = regreso + "CENTRAL";
                    path = path + "CENTRAL";
                    longitud_ = -15;
                    latitud_ = 20;
                    break;
                case 6://suroccidente
                    nombreEntrada = nombreEntrada + "SUROCCIDENTE";
                    nombreSalida = nombreSalida + "SUROCCIDENTE";
                    union = union + "SUROCCIDENTE";
                    regreso = regreso + "SUROCCIDENTE";
                    path = path + "SUROCCIDENTE";
                    longitud_ = -40;
                    latitud_ = 10;
                    break;
                case 7://noroccidente
                    nombreEntrada = nombreEntrada + "NOROCCIDENTE";
                    nombreSalida = nombreSalida + "NOROCCIDENTE";
                    union = union + "NOROCCIDENTE";
                    regreso = regreso + "NOROCCIDENTE";
                    path = path + "NOROCCIDENTE";
                    longitud_ = -40;
                    latitud_ = -20;
                    break;
                case 8://peten
                    nombreEntrada = nombreEntrada + "PETEN";
                    nombreSalida = nombreSalida + "PETEN";
                    union = union + "PETEN";
                    regreso = regreso + "PETEN";
                    path = path + "PETEN";
                    longitud_ = 10;
                    latitud_ = -50;
                    break;
            }
            //entrada de turistas por aeropuerto
            IFixedObject entrada = model.Facility.IntelligentObjects.CreateObject("Source", new FacilityLocation(longitud_ - 6, 0, latitud_ - 2)) as IFixedObject;
            entrada.ObjectName = nombreEntrada;
            entrada.Properties["InterarrivalTime"].Value = tiempoLlegada;
            entrada.Properties["EntitiesPerArrival"].Value = cantidadLlegada;
            //union de entrada de aeropuerto hacia nodo de union 
            intelligentObjects.CreateLink("Path", ((IFixedObject)model.Facility.IntelligentObjects[nombreEntrada]).Nodes[0], ((INodeObject)model.Facility.IntelligentObjects[union]), null);
            ContadorPathSimple++;
            //salida de turistas por aeropuerto
            IFixedObject salida = model.Facility.IntelligentObjects.CreateObject("Sink", new FacilityLocation(longitud_ - 6, 0, latitud_ - 4)) as IFixedObject;
            salida.ObjectName = nombreSalida;
            //union de entrada de aeropuerto hacia nodo de union 
            intelligentObjects.CreateLink("Path", (INodeObject)model.Facility.IntelligentObjects[regreso], salida.Nodes[0], null);
            model.Facility.IntelligentObjects["Path" + ContadorPathSimple].Properties["SelectionWeight"].Value = probMarcharse;
            ContadorPathSimple++;
            model.Facility.IntelligentObjects[path].Properties["SelectionWeight"].Value = probQuedarse;

        }

        public void dibujarCarnets()
        {
            int iniciox = 50, inicioy = -10, espacio = 5;

            //201503910
            dibujar2(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar0(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar1(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar5(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar0(iniciox, inicioy, espacio);
            /*iniciox = iniciox + (5 * espacio);
            dibujar3(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar9(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar1(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar0(iniciox, inicioy, espacio);*/

            iniciox = iniciox - (20 * espacio);
            inicioy = inicioy + (5 * espacio) + 8;

            //201314821
            dibujar2(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar0(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar1(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar3(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar1(iniciox, inicioy, espacio);
            /*iniciox = iniciox + (5 * espacio);
            dibujar4(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar8(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar2(iniciox, inicioy, espacio);
            iniciox = iniciox + (5 * espacio);
            dibujar1(iniciox, inicioy, espacio);*/

        }

        public void dibujar0(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n3, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n2, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n0, null) as ILinkObject;

        }

        public void dibujar1(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;

        }

        public void dibujar2(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n4, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n4, n5, null) as ILinkObject;
        }

        public void dibujar3(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n5, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n5, n4, null) as ILinkObject;

        }

        public void dibujar4(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n0, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;

        }

        public void dibujar5(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n3, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n2, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n5, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n5, n4, null) as ILinkObject;
        }

        public void dibujar6(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n4, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n4, n5, null) as ILinkObject;
            ILinkObject u5 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n5, null) as ILinkObject;
        }

        public void dibujar7(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (1 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n4, null) as ILinkObject;
        }

        public void dibujar8(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (4 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n4, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n4, n5, null) as ILinkObject;
            ILinkObject u5 = model.Facility.IntelligentObjects.CreateLink("Path", n5, n2, null) as ILinkObject;
            ILinkObject u6 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n0, null) as ILinkObject;
        }

        public void dibujar9(int iniciox, int inicioy, int espacio)
        {
            INodeObject n0 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox + (2 * espacio), 0, inicioy + (4 * espacio))) as INodeObject;

            ILinkObject u0 = model.Facility.IntelligentObjects.CreateLink("Path", n0, n1, null) as ILinkObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Path", n1, n2, null) as ILinkObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n3, null) as ILinkObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Path", n2, n5, null) as ILinkObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Path", n3, n0, null) as ILinkObject;
        }

        public void pintarMapa()
        {
            //================================================================ FRONTERA CON EL PACÍFICO //================================================================

            INodeObject p1 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(4, 0, 40)) as INodeObject;
            INodeObject p2 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-15, 0, 35)) as INodeObject;
            ILinkObject u1 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p1, p2, null) as ILinkObject;
            u1.Properties["DrawnToScale"].Value = "False";
            u1.Properties["InitialDesiredSpeed"].Value = "16.67";
            u1.Properties["LogicalLength"].Value = "56671.97";

            p1.ObjectName = "v2";
            p2.ObjectName = "v3";

            INodeObject p3 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-47, 0, 33)) as INodeObject;
            ILinkObject u2 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p2, p3, null) as ILinkObject;
            u2.Properties["DrawnToScale"].Value = "False";
            p3.ObjectName = "v4";

            u2.Properties["DrawnToScale"].Value = "False";
            u2.Properties["InitialDesiredSpeed"].Value = "16.67";
            u2.Properties["LogicalLength"].Value = "92463.27";

            INodeObject p4 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-78, 0, 14)) as INodeObject;
            ILinkObject u3 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p3, p4, null) as ILinkObject;
            p4.ObjectName = "v5";

            u3.Properties["DrawnToScale"].Value = "False";
            u3.Properties["InitialDesiredSpeed"].Value = "16.67";
            u3.Properties["LogicalLength"].Value = "104864.77";


            //================================================================ FRONTERA CON MÉXICO //================================================================

            INodeObject p5 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-72, 0, 0)) as INodeObject;
            ILinkObject u4 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p4, p5, null) as ILinkObject;
            p5.ObjectName = "v6";

            u4.Properties["DrawnToScale"].Value = "False";
            u4.Properties["InitialDesiredSpeed"].Value = "16.67";
            u4.Properties["LogicalLength"].Value = "54701.54";

            INodeObject p6 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-80, 0, -10)) as INodeObject;
            ILinkObject u5 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p5, p6, null) as ILinkObject;
            p6.ObjectName = "v7";

            u5.Properties["DrawnToScale"].Value = "False";
            u5.Properties["InitialDesiredSpeed"].Value = "16.67";
            u5.Properties["LogicalLength"].Value = "46009.63";

            INodeObject p7 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-60, 0, -40)) as INodeObject;
            ILinkObject u6 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p6, p7, null) as ILinkObject;
            p7.ObjectName = "v8";

            u6.Properties["DrawnToScale"].Value = "False";
            u6.Properties["InitialDesiredSpeed"].Value = "16.67";
            u6.Properties["LogicalLength"].Value = "129516.58";

            INodeObject p8 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-8, 0, -40)) as INodeObject;
            ILinkObject u7 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p7, p8, null) as ILinkObject;
            p8.ObjectName = "v9";

            u7.Properties["DrawnToScale"].Value = "False";
            u7.Properties["InitialDesiredSpeed"].Value = "16.67";
            u7.Properties["LogicalLength"].Value = "186768.22";

            INodeObject p9 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-14, 0, -51)) as INodeObject;
            ILinkObject u8 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p8, p9, null) as ILinkObject;
            p9.ObjectName = "v10";

            u8.Properties["DrawnToScale"].Value = "False";
            u8.Properties["InitialDesiredSpeed"].Value = "16.67";
            u8.Properties["LogicalLength"].Value = "45003.96";

            INodeObject p10 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-20, 0, -60)) as INodeObject;
            ILinkObject u9 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p9, p10, null) as ILinkObject;
            p10.ObjectName = "v11";

            u9.Properties["DrawnToScale"].Value = "False";
            u9.Properties["InitialDesiredSpeed"].Value = "16.67";
            u9.Properties["LogicalLength"].Value = "38862.16";

            INodeObject p11 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-30, 0, -65)) as INodeObject;
            ILinkObject u10 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p10, p11, null) as ILinkObject;
            p11.ObjectName = "v12";

            u10.Properties["DrawnToScale"].Value = "False";
            u10.Properties["InitialDesiredSpeed"].Value = "16.67";
            u10.Properties["LogicalLength"].Value = "40155.17";

            INodeObject p12 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-45, 0, -77)) as INodeObject;
            ILinkObject u11 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p11, p12, null) as ILinkObject;
            p12.ObjectName = "v13";

            u11.Properties["DrawnToScale"].Value = "False";
            u11.Properties["InitialDesiredSpeed"].Value = "16.67";
            u11.Properties["LogicalLength"].Value = "68996.49";

            INodeObject p13 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-30, 0, -77)) as INodeObject;
            ILinkObject u12 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p12, p13, null) as ILinkObject;
            p13.ObjectName = "v14";

            u12.Properties["DrawnToScale"].Value = "False";
            u12.Properties["InitialDesiredSpeed"].Value = "16.67";
            u12.Properties["LogicalLength"].Value = "53875.45";

            INodeObject p14 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(-30, 0, -90)) as INodeObject;
            ILinkObject u13 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p13, p14, null) as ILinkObject;
            p14.ObjectName = "v15";

            u13.Properties["DrawnToScale"].Value = "False";
            u13.Properties["InitialDesiredSpeed"].Value = "16.67";
            u13.Properties["LogicalLength"].Value = "46692.05";

            INodeObject p16 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(40, 0, -90)) as INodeObject;
            ILinkObject u15 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p14, p16, null) as ILinkObject;
            u15.Properties["DrawnToScale"].Value = "False";
            p16.ObjectName = "v16";

            u15.Properties["DrawnToScale"].Value = "False";
            u15.Properties["InitialDesiredSpeed"].Value = "16.67";
            u15.Properties["LogicalLength"].Value = "251418.76";


            //================================================================ FRONTERA CON BELICE //================================================================

            INodeObject p17 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(40, 0, -31)) as INodeObject;
            ILinkObject u16 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p16, p17, null) as ILinkObject;
            p17.ObjectName = "v17";

            u16.Properties["DrawnToScale"].Value = "False";
            u16.Properties["InitialDesiredSpeed"].Value = "16.67";
            u16.Properties["LogicalLength"].Value = "200947.50";

            INodeObject p18 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(59, 0, -29)) as INodeObject;
            ILinkObject u17 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p17, p18, null) as ILinkObject;
            p18.ObjectName = "v18";

            u17.Properties["DrawnToScale"].Value = "False";
            u17.Properties["InitialDesiredSpeed"].Value = "16.67";
            u17.Properties["LogicalLength"].Value = "65052.50";


            //================================================================ FRONTERA CON EL ATLÁNTICO //================================================================

            INodeObject p19 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(44, 0, -20)) as INodeObject;
            ILinkObject u18 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p18, p19, null) as ILinkObject;
            p19.ObjectName = "v19";

            u18.Properties["DrawnToScale"].Value = "False";
            u18.Properties["InitialDesiredSpeed"].Value = "16.67";
            u18.Properties["LogicalLength"].Value = "25415.02";


            INodeObject p20 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(41, 0, -21)) as INodeObject;
            ILinkObject u19 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p19, p20, null) as ILinkObject;
            p20.ObjectName = "v20";

            u19.Properties["DrawnToScale"].Value = "False";
            u19.Properties["InitialDesiredSpeed"].Value = "16.67";
            u19.Properties["LogicalLength"].Value = "4591.85";

            INodeObject p21 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(39, 0, -19)) as INodeObject;
            ILinkObject u20 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p20, p21, null) as ILinkObject;
            p21.ObjectName = "v21";

            u20.Properties["DrawnToScale"].Value = "False";
            u20.Properties["InitialDesiredSpeed"].Value = "16.67";
            u20.Properties["LogicalLength"].Value = "4112.32";

            INodeObject p22 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(36, 0, -20)) as INodeObject;
            ILinkObject u21 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p21, p22, null) as ILinkObject;
            p22.ObjectName = "v22";

            u21.Properties["DrawnToScale"].Value = "False";
            u21.Properties["InitialDesiredSpeed"].Value = "16.67";
            u21.Properties["LogicalLength"].Value = "4591.85";

            INodeObject p23 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(34, 0, -18)) as INodeObject;
            ILinkObject u22 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p22, p23, null) as ILinkObject;
            p23.ObjectName = "v23";

            u22.Properties["DrawnToScale"].Value = "False";
            u22.Properties["InitialDesiredSpeed"].Value = "16.67";
            u22.Properties["LogicalLength"].Value = "4112.32";

            INodeObject p24 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(37, 0, -16)) as INodeObject;
            ILinkObject u23 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p23, p24, null) as ILinkObject;
            p24.ObjectName = "v24";

            u23.Properties["DrawnToScale"].Value = "False";
            u23.Properties["InitialDesiredSpeed"].Value = "16.67";
            u23.Properties["LogicalLength"].Value = "5245.75";

            INodeObject p25 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(38, 0, -13)) as INodeObject;
            ILinkObject u24 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p24, p25, null) as ILinkObject;
            p25.ObjectName = "v25";

            u24.Properties["DrawnToScale"].Value = "False";
            u24.Properties["InitialDesiredSpeed"].Value = "16.67";
            u24.Properties["LogicalLength"].Value = "4591.85";

            INodeObject p26 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(41, 0, -15)) as INodeObject;
            ILinkObject u25 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p25, p26, null) as ILinkObject;
            p26.ObjectName = "v26";

            u25.Properties["DrawnToScale"].Value = "False";
            u25.Properties["InitialDesiredSpeed"].Value = "16.67";
            u25.Properties["LogicalLength"].Value = "5245.75";

            INodeObject p27 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(51, 0, -18)) as INodeObject;
            ILinkObject u26 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p26, p27, null) as ILinkObject;
            p27.ObjectName = "v27";

            u26.Properties["DrawnToScale"].Value = "False";
            u26.Properties["InitialDesiredSpeed"].Value = "16.67";
            u26.Properties["LogicalLength"].Value = "15170.54";

            INodeObject p28 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(52, 0, -22)) as INodeObject;
            ILinkObject u27 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p27, p28, null) as ILinkObject;
            p28.ObjectName = "v28";

            u27.Properties["DrawnToScale"].Value = "False";
            u27.Properties["InitialDesiredSpeed"].Value = "16.67";
            u27.Properties["LogicalLength"].Value = "5986.84";

            INodeObject p29 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(61, 0, -28)) as INodeObject;
            ILinkObject u28 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p28, p29, null) as ILinkObject;
            p29.ObjectName = "v29";

            u28.Properties["DrawnToScale"].Value = "False";
            u28.Properties["InitialDesiredSpeed"].Value = "16.67";
            u28.Properties["LogicalLength"].Value = "15722.73";

            INodeObject p30 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(65, 0, -25)) as INodeObject;
            ILinkObject u29 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p29, p30, null) as ILinkObject;
            p30.ObjectName = "v30";

            u29.Properties["DrawnToScale"].Value = "False";
            u29.Properties["InitialDesiredSpeed"].Value = "16.67";
            u29.Properties["LogicalLength"].Value = "7265.59";

            INodeObject p31 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(67, 0, -27)) as INodeObject;
            ILinkObject u30 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p30, p31, null) as ILinkObject;
            p31.ObjectName = "v31";

            u30.Properties["DrawnToScale"].Value = "False";
            u30.Properties["InitialDesiredSpeed"].Value = "16.67";
            u30.Properties["LogicalLength"].Value = "4112.32";

            INodeObject p32 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(64, 0, -30)) as INodeObject;
            ILinkObject u31 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p31, p32, null) as ILinkObject;
            p32.ObjectName = "v32";

            u31.Properties["DrawnToScale"].Value = "False";
            u31.Properties["InitialDesiredSpeed"].Value = "16.67";
            u31.Properties["LogicalLength"].Value = "6161.22";

            INodeObject p33 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(69, 0, -29)) as INodeObject;
            ILinkObject u32 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p32, p33, null) as ILinkObject;
            p33.ObjectName = "v33";

            u32.Properties["DrawnToScale"].Value = "False";
            u32.Properties["InitialDesiredSpeed"].Value = "16.67";
            u32.Properties["LogicalLength"].Value = "7410.90";

            INodeObject p34 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(63, 0, -33)) as INodeObject;
            ILinkObject u33 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p33, p34, null) as ILinkObject;
            p34.ObjectName = "v34";

            u33.Properties["DrawnToScale"].Value = "False";
            u33.Properties["InitialDesiredSpeed"].Value = "16.67";
            u33.Properties["LogicalLength"].Value = "10476.98";

            INodeObject p35 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(65, 0, -34)) as INodeObject;
            ILinkObject u34 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p34, p35, null) as ILinkObject;
            p35.ObjectName = "v35";

            u34.Properties["DrawnToScale"].Value = "False";
            u34.Properties["InitialDesiredSpeed"].Value = "16.67";
            u34.Properties["LogicalLength"].Value = "3254.98";

            INodeObject p36 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(73, 0, -28)) as INodeObject;
            ILinkObject u35 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p35, p36, null) as ILinkObject;
            p36.ObjectName = "v36";

            u35.Properties["DrawnToScale"].Value = "False";
            u35.Properties["InitialDesiredSpeed"].Value = "16.67";
            u35.Properties["LogicalLength"].Value = "14531.17";


            //================================================================ FRONTERA CON HONDURAS //================================================================

            INodeObject p37 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(45, 0, -5)) as INodeObject;
            ILinkObject u36 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p36, p37, null) as ILinkObject;
            p37.ObjectName = "v37";

            u36.Properties["DrawnToScale"].Value = "False";
            u36.Properties["InitialDesiredSpeed"].Value = "16.67";
            u36.Properties["LogicalLength"].Value = "129212.26";

            INodeObject p38 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(42, 0, 2)) as INodeObject;
            ILinkObject u37 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p37, p38, null) as ILinkObject;
            p38.ObjectName = "v38";

            u37.Properties["DrawnToScale"].Value = "False";
            u37.Properties["InitialDesiredSpeed"].Value = "16.67";
            u37.Properties["LogicalLength"].Value = "27168.80";

            INodeObject p39 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(44, 0, 13)) as INodeObject;
            ILinkObject u38 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p38, p39, null) as ILinkObject;
            p39.ObjectName = "v39";

            u38.Properties["DrawnToScale"].Value = "False";
            u38.Properties["InitialDesiredSpeed"].Value = "16.67";
            u38.Properties["LogicalLength"].Value = "39861.84";

            INodeObject p40 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(28, 0, 18)) as INodeObject;
            ILinkObject u39 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p39, p40, null) as ILinkObject;
            p40.ObjectName = "v40";

            u39.Properties["DrawnToScale"].Value = "False";
            u39.Properties["InitialDesiredSpeed"].Value = "16.67";
            u39.Properties["LogicalLength"].Value = "59757.10";

            INodeObject p41 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(27, 0, 24)) as INodeObject;
            ILinkObject u40 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p40, p41, null) as ILinkObject;
            p41.ObjectName = "v41";

            u40.Properties["DrawnToScale"].Value = "False";
            u40.Properties["InitialDesiredSpeed"].Value = "16.67";
            u40.Properties["LogicalLength"].Value = "35889.50";

            INodeObject p42 = model.Facility.IntelligentObjects.CreateObject("BasicNode", new FacilityLocation(12, 0, 32)) as INodeObject;
            ILinkObject u41 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p41, p42, null) as ILinkObject;
            p42.ObjectName = "v42";

            u41.Properties["DrawnToScale"].Value = "False";
            u41.Properties["InitialDesiredSpeed"].Value = "16.67";
            u41.Properties["LogicalLength"].Value = "100348.94";

            ILinkObject u42 = model.Facility.IntelligentObjects.CreateLink("Conveyor", p42, p1, null) as ILinkObject;

            u42.Properties["DrawnToScale"].Value = "False";
            u42.Properties["InitialDesiredSpeed"].Value = "16.67";
            u42.Properties["LogicalLength"].Value = "66761.56";

            IFixedObject base_militar = model.Facility.IntelligentObjects.CreateObject("Source", new FacilityLocation(0, 0, -70)) as IFixedObject;
            ILinkObject u43 = model.Facility.IntelligentObjects.CreateLink("Path", base_militar.Nodes[0], p14, null) as ILinkObject;
            base_militar.ObjectName = "BaseMilitar";
            base_militar.Properties["InterarrivalTime"].Value = "15";
            base_militar.Properties["MaximumArrivals"].Value = "15";

        }


    }
}
