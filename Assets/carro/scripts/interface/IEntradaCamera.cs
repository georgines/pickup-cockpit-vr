namespace Vehicle
{
    /// <summary>
    /// Contrato que qualquer fonte de input da câmera deve implementar.
    /// O CameraSeguimentoSimples não sabe quem está fornecendo os valores.
    /// </summary>
    public interface IEntradaCamera
    {
        /// <summary>Movimento horizontal/vertical da câmera. -1..1 em cada eixo.</summary>
        UnityEngine.Vector2 Olhar { get; }

        /// <summary>Alternância entre câmera externa e interna neste frame.</summary>
        bool AlternarModo { get; }
    }
}
