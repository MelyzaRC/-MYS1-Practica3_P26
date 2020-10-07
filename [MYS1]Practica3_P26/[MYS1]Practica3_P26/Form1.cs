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
                dibujarCarnets();
                try
                {
                    SimioProjectFactory.SaveProject(_ProyectoSimio, "ModeloModificado.spfx", out warnings);
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
            crearRegion("norte", -20, 5, "50", "Random.Poisson(8)", "minutos","Random.Exponential(5)");
            crearRegion("nororiente", -10, 30, "40", "Random.Poisson(6)", "minutos","Random.Exponential(3)");
            crearRegion("suroriente", 20, 15, "30", "Random.Poisson(10)", "minutos", "Random.Exponential(4)");
            crearRegion("central", 20, -15, "100", "Random.Poisson(3)", "minutos", "Random.Exponential(5)");
            crearRegion("suroccidente", 10, -40, "120", "Random.Poisson(4)", "minutos", "Random.Exponential(3)");
            crearRegion("noroccidente", -20, -40, "30", "Random.Poisson(12)", "minutos", "Random.Exponential(6)");
            crearRegion("peten", -50, 10, "150", "Random.Poisson(4)", "minutos", "Random.Exponential(4)");
        }

        public void crearRegion(string nombre,  long latitud_,long longitud_, string capacidad_, string tiempoLlegada_, string unidadTiempo, string tiempoAtencion_)
        {
            //Server que simula la estacion de servicio ubicada en cada region 
            IFixedObject estacion = model.Facility.IntelligentObjects.CreateObject("Server", new FacilityLocation( longitud_, 0, latitud_)) as IFixedObject;
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

            crearEnlace(2,2,"0.40","0");
            crearEnlace(2,8,"0.40","147000");
            crearEnlace(2,3,"0.10","138000");
            crearEnlace(2,7,"0.10","145000");

            crearEnlace(3,3,"0.20","0");
            crearEnlace(3,1,"0.30","241000");
            crearEnlace(3,2,"0.15","138000");
            crearEnlace(3,4,"0.05","231000");
            crearEnlace(3,8,"0.30","282000");

            crearEnlace(4,4,"0.40","0");
            crearEnlace(4,3,"0.20", "231000");
            crearEnlace(4,1,"0.25", "124000");
            crearEnlace(4,5, "0.15", "154000");

            crearEnlace(5,5, "0.35", "0");
            crearEnlace(5,1, "0.35", "63000");
            crearEnlace(5,4, "0.05", "154000");
            crearEnlace(5,6, "0.15", "155000");
            crearEnlace(5,7, "0.10", "269000");

            crearEnlace(6,6, "0.35", "0");
            crearEnlace(6,7, "0.30", "87000");
            crearEnlace(6,5, "0.35", "155000");

            crearEnlace(7,7, "0.40", "0");
            crearEnlace(7,6, "0.30", "87000");
            crearEnlace(7,5, "0.10", "269000");
            crearEnlace(7,2, "0.20", "145000");

            crearEnlace(8,8, "0.5", "0");
            crearEnlace(8,2, "0.25", "147000");
            crearEnlace(8,3, "0.25", "282000");
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
            crearAeropuerto(1, "70", "Math.Round(Random.Exponential(35))","0.50","0.50");
            crearAeropuerto(8, "40", "Math.Round(Random.Exponential(50))","0.30", "0.70");
            crearAeropuerto(7, "30", "Math.Round(Random.Exponential(70))", "0.40","0.60");
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
                    latitud_ = - 10;
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
                    longitud_ = - 15;
                    latitud_ = 20;
                    break;
                case 6://suroccidente
                    nombreEntrada = nombreEntrada + "SUROCCIDENTE";
                    nombreSalida = nombreSalida + "SUROCCIDENTE";
                    union = union + "SUROCCIDENTE";
                    regreso = regreso + "SUROCCIDENTE";
                    path = path + "SUROCCIDENTE";
                    longitud_ = - 40;
                    latitud_ = 10;
                    break;
                case 7://noroccidente
                    nombreEntrada = nombreEntrada + "NOROCCIDENTE";
                    nombreSalida = nombreSalida + "NOROCCIDENTE";
                    union = union + "NOROCCIDENTE";
                    regreso = regreso + "NOROCCIDENTE";
                    path = path + "NOROCCIDENTE";
                    longitud_ = - 40;
                    latitud_ = - 20;
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
            intelligentObjects.CreateLink("Path", (INodeObject)model.Facility.IntelligentObjects[regreso],salida.Nodes[0], null);
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

            iniciox = iniciox - (20* espacio);
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
            INodeObject n0= model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy)) as INodeObject;
            INodeObject n1 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox +(2*espacio), 0, inicioy)) as INodeObject;
            INodeObject n2 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox+(2*espacio), 0, inicioy+(2*espacio))) as INodeObject;
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy+(2*espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox, 0, inicioy+(4*espacio))) as INodeObject;
            INodeObject n5 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox+(2*espacio), 0, inicioy+(4*espacio))) as INodeObject;
           
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
            INodeObject n3 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox+(1*espacio), 0, inicioy + (2 * espacio))) as INodeObject;
            INodeObject n4 = model.Facility.IntelligentObjects.CreateObject("TransferNode", new FacilityLocation(iniciox+(2*espacio), 0, inicioy + (4 * espacio))) as INodeObject;
            
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


    }
}
