namespace Vehicle
{
    /// <summary>
    /// Contrato que qualquer rádio do carro deve implementar.
    /// O ControleCarro não sabe quem está tocando o som —
    /// pode ser TocadorSomCarro, um rádio de streaming, silêncio, etc.
    /// </summary>
    public interface IRadioCarro
    {
        /// <summary>Liga ou desliga o som. A música continua tocando internamente.</summary>
        void LigarEDesligarRadio();

        /// <summary>Pula para um ponto aleatório da faixa atual.</summary>
        void PularEstacao();
    }
}
