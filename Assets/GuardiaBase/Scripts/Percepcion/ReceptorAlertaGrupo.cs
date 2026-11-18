using System.Collections;
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class ReceptorAlertaGrupo : MonoBehaviour
    {
        [SerializeField] private float latenciaBase = 1f;
        [SerializeField] private float latenciaPorMetro = 0.1f;

        private GuardiaAgente _agente;

        private void Awake() { _agente = GetComponent<GuardiaAgente>(); }

        private void OnEnable() { AlertaGrupo.AlEmitir += AlRecibir; }
        private void OnDisable() { AlertaGrupo.AlEmitir -= AlRecibir; }

        private void AlRecibir(GameObject emisor, Vector3 posicion)
        {
            if (emisor == gameObject) return;

            float espera = latenciaBase +
                           Vector3.Distance(transform.position, posicion) * latenciaPorMetro;
            StartCoroutine(Aplicar(posicion, espera));
        }

        private IEnumerator Aplicar(Vector3 posicion, float espera)
        {
            yield return new WaitForSeconds(espera);

            Blackboard pizarra = _agente.Pizarra;
            if (pizarra == null) yield break;

            // Si ya lo veo yo, mi información es mejor que el aviso.
            if (pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Alerta)
                yield break;

            pizarra.Set(ClavesBB.UltimaPos, posicion);
            pizarra.Set(ClavesBB.TieneUltimaPos, true);
            pizarra.Set(ClavesBB.AlertaDeGrupo, true);
        }
    }
}