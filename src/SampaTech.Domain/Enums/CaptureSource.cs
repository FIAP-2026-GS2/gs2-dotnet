namespace SampaTech.Domain.Enums;

public enum CaptureSource
{
    RECORDING_FRAME, // frame de horário agendado (12:00 ou 00:00)
    CITIZEN,         // foto enviada pelo cidadão (via API Java)
    EVENT            // frame extraído durante um alerta de chuva
}
