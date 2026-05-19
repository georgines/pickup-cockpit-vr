namespace Vehicle
{
    /// <summary>
    /// Contrato que qualquer fonte de input do carro deve implementar.
    /// O ControleCarro não sabe quem está fornecendo os valores —
    /// pode ser teclado, gamepad, VR, IA, rede, etc.
    /// </summary>
    public interface IEntradaCarro
    {
        /// <summary>Aceleração para frente. 0 = nenhuma, 1 = máxima.</summary>
        float Aceleracao { get; }

        /// <summary>Freio / ré. 0 = nenhum, 1 = máximo. Parado = engata ré.</summary>
        float Freio { get; }

        /// <summary>Direção. -1 = toda esquerda, 0 = reto, 1 = toda direita.</summary>
        float Direcao { get; }

        /// <summary>Freio de mão ativo.</summary>
        bool FreioMao { get; }

        /// <summary>Buzina pressionada neste frame.</summary>
        bool Buzina { get; }

        /// <summary>Respawn solicitado neste frame.</summary>
        bool Respawn { get; }

        /// <summary>Liga ou desliga o rádio (mute/unmute) neste frame.</summary>
        bool AlternarRadio { get; }

        /// <summary>Pular para ponto aleatório da música neste frame.</summary>
        bool PularEstacao { get; }
    }
}
