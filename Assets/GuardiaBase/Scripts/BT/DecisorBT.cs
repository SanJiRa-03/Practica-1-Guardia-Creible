// ═══════════════════════════════════════════════════════════════════════════
//  DecisorBT.cs — Cerebro versión 2: árbol de comportamiento
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  APARTADO C DEL ENUNCIADO.
//
//  El runtime (BehaviorTree.cs) OS LO DOY. No lo modifiquéis: en la sesión 9
//  lo explicamos línea a línea y en la memoria os preguntaré por él.
//
//  Lo que escribís vosotros es lo de este fichero: LA FORMA DEL ÁRBOL y LOS
//  NODOS HOJA. Eso es lo que se evalúa.
//
//  ⚠️ La condición del apartado D: este decisor tiene que producir el MISMO
//  comportamiento observable que DecisorFSM, leyendo la MISMA pizarra, sin que
//  haga falta tocar ni SensorVision ni LocomocionNavMesh. Si para que funcione
//  el árbol necesitáis cambiar algo de la percepción, algo está mal colocado.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorBT : IDecisor
    {
        private readonly GuardiaAgente _agente;
        private BehaviorTree _arbol;

        public DecisorBT(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            _arbol = new BehaviorTree(ConstruirArbol(), bb);
        }

        public void Decidir(Blackboard bb)
        {
            if (_arbol != null) _arbol.Tick();
        }

        public string Traza { get { return "BT: " + Trazador.Ultima(); } }

        // ───────────────────────────────────────────────────────────────────
        //  EL ÁRBOL — ESTO ES VUESTRO
        // ───────────────────────────────────────────────────────────────────

        private Node ConstruirArbol()
        {
            // Lo que hay aquí abajo es un ÁRBOL DE JUGUETE: solo sabe patrullar.
            // Sirve para que el proyecto compile y arranque, y para que veáis
            // cómo se enchufa un nodo hoja. Nada más.
            //
            // Vuestro árbol tiene que cubrir los cuatro comportamientos del §3
            // del enunciado. La forma canónica, la que vimos en la sesión 8, es
            // un ActiveSelector con las ramas ORDENADAS POR PRIORIDAD:
            //
            //   ActiveSelector "Guardia"
            //     ├── Monitor  guarda: ¿nivel == Alerta?     → Perseguir
            //     ├── Monitor  guarda: ¿nivel == Sospecha?   → Investigar
            //     ├── Sequence ¿tengo última posición?       → Buscar
            //     └── Patrullar
            //
            // TRES AVISOS, por orden de cuánta gente tropieza con ellos:
            //
            //  1. Usad ActiveSelector, no Selector. Un Selector normal se queda
            //     pegado al hijo que devolvió Running y no vuelve a mirar si una
            //     rama de mayor prioridad se ha vuelto posible. Con Selector, el
            //     guardia termina la patrulla ANTES de reaccionar a veros.
            //
            //  2. Usad Monitor, no Sequence, cuando la condición tenga que
            //     seguir cumpliéndose durante toda la acción. Una Sequence con
            //     la condición delante NO la vuelve a comprobar: cuando está
            //     Running retoma en la acción. El resultado es un guardia que
            //     persigue eternamente a un jugador al que ya no ve.
            //     Probadlo: cambiad un Monitor por una Sequence y mirad la traza
            //     con "Mostrar Traza" activado. Ese experimento es material de
            //     memoria.
            //
            //  3. Un nodo hoja NO guarda «en qué estado estoy». Si os pilláis
            //     escribiendo un bool _persiguiendo dentro de un nodo, habéis
            //     hecho una FSM disfrazada de árbol, y eso está en la lista de
            //     errores que cuestan nota.

            return new ActiveSelector("Guardia",

                // ── ✅ hecho · rama de ALERTA (perseguir) ───────────────────────
                new Monitor("SiAlerta",
                    new Condition("¿Alerta?", bb => bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Alerta),
                    new ActionNode("Perseguir", bb =>
                    {
                        bb.Set(ClavesBB.Destino, bb.Get(ClavesBB.UltimaPos, Vector3.zero));
                        bb.Set(ClavesBB.TieneDestino, true);

                        return Status.Running;
                    }
                    )),

                // ── Rama de AUDICIÓN (ruido) ────────────────────────────────────
                new Sequence("SiRuido",
                    new Condition("¿Tiene ruido pendiente?", bb => bb.Get(ClavesBB.TieneRuidoPendiente, false)),
                    new ActionNode("Atender ruido", bb =>
                    {
                        bb.Set(ClavesBB.TieneRuidoPendiente, false);
                        return Status.Success;
                    }
                    ),
                    new IrA("Ir a posición del ruido", ClavesBB.PosicionRuido)),

                // ── ✅ hecho · rama de SOSPECHA (investigar) ────────────────────
                new Monitor("SiSospecha",
                    new Condition("¿Sospecha?", bb => bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Sospecha),
                    new IrA("Ir a última posición", ClavesBB.UltimaPos)),

                // ── ✅ hecho · rama de BÚSQUEDA (última posición conocida) ──────
                new Sequence("SiÚltimaPos",
                    new Condition("¿Tiene última posición?", bb => bb.Get(ClavesBB.TieneUltimaPos, false)),
                    new IrA("Ir a última posición", ClavesBB.UltimaPos),
                    new MirarAlrededor("Mirar alrededor")),

                // ── Rama de PATRULLA ────────────────────────────────────────────
                new ActionNode("Patrullar", bb =>
                {
                    RutaPatrulla ruta = _agente.GetComponent<RutaPatrulla>();
                    if (ruta == null || !ruta.TieneRuta) return Status.Failure;

                    // Si ya llegué al punto anterior, pido el siguiente.
                    if (bb.Get(ClavesBB.HaLlegado, false))
                    {
                        bb.Set(ClavesBB.Destino, ruta.Avanzar());
                        bb.Set(ClavesBB.TieneDestino, true);
                    }

                    // Patrullar no «termina» nunca: es una acción de fondo.
                    return Status.Running;
                })
            );
        }
    }

    //Creamos un nodo hoja que nos permite ir a un destino y esperar a que llegue  (Rama de SOSPECHA).
    public sealed class IrA: Node
    {
        //La clave del blackboard donde está el destino.
        private readonly string clave;
        //El constructor recibe el nombre del nodo y la clave del blackboard.
        public IrA(string nombre, string clave) : base(nombre)
        {
            this.clave = clave;
        }

        protected override void OnInitialize(Blackboard bb)
        {        
            //Cogemos lo que haya en clave, si no hay nada, usamos el punto (0,0,0). Y luego lo guardamos en Destino.
            bb.Set(ClavesBB.Destino, bb.Get(clave, Vector3.zero));
            //Cuando se inicializa el nodo, ponemos el destino en el blackboard.
            bb.Set(ClavesBB.TieneDestino, true);
            //Borra el "ya llegué" de la vez anterior para que el Update siga funcionando.
            bb.Set(ClavesBB.HaLlegado, false);
        }
        protected override Status Update(Blackboard bb)
        {
            if (bb.Get(ClavesBB.HaLlegado, false))
            {
                return Status.Success;
            }
            else
            {
                return Status.Running;
            }
        }
    }
    //Creamos un nodo hoja que nos permite mirar alrededor durante un tiempo determinado (Rama de BÚSQUEDA).
    public sealed class MirarAlrededor : Node
    {
        private float horaInicio;
        private float horaLlegada;

        private float tiempoMaximo = 12f; //Tiempo máximo para mirar alrededor, (se le da tiempo para revisar todos los puntos).
        private float tiempoDeEspera; //Tiempo de espera en cada punto.
        private float radioBusqueda = 4f; //Radio de búsqueda.
        public MirarAlrededor(string nombre) : base(nombre)
        {
        }
        protected override void OnInitialize(Blackboard bb)
        {
            horaInicio = bb.Tiempo; //Guarda la hora de inicio
            horaLlegada = -1f; //Inicializa la hora de llegada a -1 para indicar que todavía no.
            tiempoDeEspera = Random.Range(1f, 3f);
        }
        protected override Status Update(Blackboard bb)
        {
            if (bb.Tiempo - horaInicio >= tiempoMaximo)
            {
                bb.Set(ClavesBB.TieneUltimaPos, false); //Borra la última posición conocida.
                return Status.Success; //Ha terminado de mirar alrededor
            }
            else if (bb.Get(ClavesBB.HaLlegado, false))
            {
                if (horaLlegada < 0f)
                {
                    horaLlegada = bb.Tiempo; //Guarda la hora de llegada para empezar a contar el tiempo de espera.
                }
                else if (bb.Tiempo - horaLlegada >= tiempoDeEspera)
                {
                    Vector2 puntoAleatorio2D = Random.insideUnitCircle * radioBusqueda; //Genera un punto aleatorio dentro de un círculo unitario 
                    Vector3 puntoAleatorio3D = new Vector3(puntoAleatorio2D.x, 0f, puntoAleatorio2D.y); //Convierte el punto 2D en 3D, la y del círculo pasa a ser la z del mundo.

                    Vector3 nuevaPosicion = bb.Get(ClavesBB.UltimaPos, Vector3.zero) + puntoAleatorio3D; //Suma el punto aleatorio a la última posición conocida.

                    bb.Set(ClavesBB.Destino, nuevaPosicion);
                    bb.Set(ClavesBB.TieneDestino, true);
                    bb.Set(ClavesBB.HaLlegado, false);

                    horaLlegada = -1f; //Reinicia la hora de llegada para que vuelva a esperar en el siguiente punto.
                    tiempoDeEspera = Random.Range(1f, 3f);
                }

                return Status.Running; //Sigue mirando alrededor.
            }
            else
            {
                return Status.Running; //Sigue mirando alrededor
            }
        }
    }
}
