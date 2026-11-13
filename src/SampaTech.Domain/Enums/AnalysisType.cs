namespace SampaTech.Domain.Enums;

// O que a IA deve analisar num frame. Sai do horário da agenda (ou do alerta de chuva).
public enum AnalysisType
{
    POTHOLE_CROSSWALK, // buraco e faixa apagada (frame do meio-dia)
    STREETLIGHT,       // poste: meio-dia ("de dia") comparado com meia-noite ("de noite")
    FLOODING           // via alagada (frames durante alerta de chuva)
}
