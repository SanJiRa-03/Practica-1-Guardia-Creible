// ═══════════════════════════════════════════════════════════════════════════
//  RutaPatrulla.cs — Los puntos de patrulla
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  MATERIAL DE PARTIDA — terminado. Podéis ampliarlo (ver §7 del enunciado:
//  waypoints como objetos inteligentes que se anuncian solos).
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class RutaPatrulla : MonoBehaviour
    {
        [Tooltip("Los puntos, en orden. Arrastradlos aquí desde la jerarquía.")]
        public Transform[] Puntos;

        [Tooltip("Ida y vuelta en lugar de circular.")]
        public bool PingPong = false;

        [Tooltip("Empezar por el punto más cercano en vez de por el primero.")]
        public bool EmpezarPorElMasCercano = true;

        private int _indice;
        private int _paso = 1;

        // ── Waypoints inteligentes: SOLO memoria. Decide EstadoPatrullar. ──
        private float[] _ultimaVisita;
        private float[] _interes;

        [Header("Waypoints inteligentes")]
        [Tooltip("Radio en el que un suceso afecta a los waypoints cercanos.")]
        public float RadioInteres = 12f;
        [Tooltip("Cuánto interés pierde un waypoint por segundo.")]
        public float DecaimientoInteres = 0.02f;

        public int NumPuntos { get { return TieneRuta ? Puntos.Length : 0; } }
        public int IndiceActual { get { return _indice; } }

        private void IniciarMemoria()
        {
            if (_ultimaVisita != null && _ultimaVisita.Length == Puntos.Length) return;
            _ultimaVisita = new float[Puntos.Length];
            _interes = new float[Puntos.Length];
        }

        public float TiempoSinVisitar(int i) { IniciarMemoria(); return Time.time - _ultimaVisita[i]; }
        public float Interes(int i) { IniciarMemoria(); return _interes[i]; }
        public Vector3 Posicion(int i) { return Puntos[i].position; }

        /// <summary>Marca el punto actual como visitado, salta al punto i y devuelve su posición.</summary>
        public Vector3 IrA(int i)
        {
            IniciarMemoria();
            _ultimaVisita[_indice] = Time.time;
            _interes[_indice] = 0f;          // ya lo he comprobado: se apaga el interés
            _indice = i;
            return PuntoActual;
        }

        /// <summary>Sube el interés de los waypoints cercanos a una posición (algo pasó ahí).</summary>
        public void MarcarInteres(Vector3 posicion, float cantidad)
        {
            if (!TieneRuta) return;
            IniciarMemoria();

            for (int i = 0; i < Puntos.Length; i++)
            {
                if (Puntos[i] == null) continue;
                if (Vector3.Distance(Puntos[i].position, posicion) <= RadioInteres)
                    _interes[i] = Mathf.Min(1f, _interes[i] + cantidad);
            }
        }

        private void Update()
        {
            if (!TieneRuta) return;
            IniciarMemoria();

            for (int i = 0; i < _interes.Length; i++)
                _interes[i] = Mathf.Max(0f, _interes[i] - DecaimientoInteres * Time.deltaTime);
        }

        private void Start()
        {
            if (EmpezarPorElMasCercano) _indice = IndiceMasCercano(transform.position);
        }

        public bool TieneRuta { get { return Puntos != null && Puntos.Length > 0; } }

        /// <summary>El punto al que toca ir ahora.</summary>
        public Vector3 PuntoActual
        {
            get
            {
                if (!TieneRuta) return transform.position;
                return Puntos[Mathf.Clamp(_indice, 0, Puntos.Length - 1)].position;
            }
        }

        /// <summary>Pasa al siguiente punto y lo devuelve.</summary>
        public Vector3 Avanzar()
        {
            if (!TieneRuta) return transform.position;

            if (PingPong)
            {
                if (_indice + _paso >= Puntos.Length || _indice + _paso < 0) _paso = -_paso;
                _indice += _paso;
            }
            else
            {
                _indice = (_indice + 1) % Puntos.Length;
            }

            return PuntoActual;
        }

        private int IndiceMasCercano(Vector3 desde)
        {
            int mejor = 0;
            float mejorD = float.MaxValue;

            for (int i = 0; i < Puntos.Length; i++)
            {
                if (Puntos[i] == null) continue;
                float d = (Puntos[i].position - desde).sqrMagnitude;
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            return mejor;
        }

        private void OnDrawGizmos()
        {
            if (!TieneRuta) return;

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);

            for (int i = 0; i < Puntos.Length; i++)
            {
                if (Puntos[i] == null) continue;

                Gizmos.DrawWireSphere(Puntos[i].position, 0.35f);

                Transform sig = Puntos[(i + 1) % Puntos.Length];
                if (sig != null && (PingPong == false || i + 1 < Puntos.Length))
                    Gizmos.DrawLine(Puntos[i].position, sig.position);
            }
        }
    }
}
