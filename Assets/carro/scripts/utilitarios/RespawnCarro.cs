using UnityEngine;
using UnityEngine.Serialization;

namespace Vehicle
{
    /// <summary>
    /// Guarda o ponto de spawn do carro e executa o respawn quando solicitado.
    /// Não conhece nenhuma entrada — quem decide quando respawnar é o
    /// ControleCarro (via IEntradaCarro) ou outro sistema externo.
    /// </summary>
    public class RespawnCarro : MonoBehaviour
    {
        [FormerlySerializedAs("fallYThreshold")]
        public float limiteQuedaY = -20f;

        private Vector3    posicaoSpawn;
        private Quaternion rotacaoSpawn;
        private Rigidbody  rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            SalvarPontoSpawn();
        }

        private void LateUpdate()
        {
            // Respawn automático por queda
            if (transform.position.y < limiteQuedaY)
                Respawnar();
        }

        /// <summary>Salva a posição e rotação atuais como ponto de spawn.</summary>
        public void SalvarPontoSpawn()
        {
            posicaoSpawn = transform.position;
            rotacaoSpawn = transform.rotation;
        }

        /// <summary>Alias em inglês mantido para compatibilidade com o setup tool.</summary>
        public void SaveSpawnPoint() => SalvarPontoSpawn();

        /// <summary>
        /// Teleporta o carro para o ponto de spawn e zera a velocidade.
        /// Chamado pelo ControleCarro ou qualquer sistema externo.
        /// </summary>
        public void Respawnar()
        {
            if (rb != null)
            {
                rb.position = posicaoSpawn;
                rb.rotation = rotacaoSpawn;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
                rb.WakeUp();
            }
            else
            {
                transform.SetPositionAndRotation(posicaoSpawn, rotacaoSpawn);
            }
        }
    }
}
