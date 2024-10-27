namespace Domain.Entities;

public class KeyPair : _BaseEntity
{
    public KeyPair(Guid surveyId, string privateKey, string publicKey)
    {
        SurveyId = surveyId;
        PrivateKey = privateKey;
        PublicKey = publicKey;
    }

    public Guid SurveyId { get; set; }

    public string PrivateKey { get; set; }

    public string PublicKey { get; set; }
}