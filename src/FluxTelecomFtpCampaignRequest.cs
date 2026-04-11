using System;

namespace Sufficit.Gateway.FluxTelecom.SMS
{
    /// <summary>
    /// Request used to trigger campaign generation from an uploaded FTP file.
    /// </summary>
    public class FluxTelecomFtpCampaignRequest
    {
        /// <summary>
        /// Integration identifier of the uploaded source file.
        /// </summary>
        public int IntegrationId { get; set; }

        /// <summary>
        /// Portal-specific proposal type posted as <c>campanhaVo.tipoPropostaFTP</c>.
        /// </summary>
        public uint ProposalType { get; set; }

        /// <summary>
        /// Remaining message quantity sent as <c>integracaoVo.qtdMensagemAux</c>.
        /// </summary>
        public uint RemainingMessages { get; set; }

        /// <summary>
        /// Validates the request before it is posted to the portal.
        /// </summary>
        public void Validate()
        {
            if (IntegrationId <= 0)
                throw new ArgumentOutOfRangeException(nameof(IntegrationId), "IntegrationId must be greater than zero.");
        }
    }
}
