using FiapConnect.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FiapConnect.Infrastructure.Mongo.Mapeamentos;

public static class NotificacaoMap
{
    public static void Registrar()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Notificacao))) return;

        BsonClassMap.RegisterClassMap<Notificacao>(cm =>
        {
            cm.AutoMap();

            cm.MapIdMember(n => n.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId))
              .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance);

            cm.MapMember(n => n.RmDestinatario).SetElementName("rm_destinatario");
            cm.MapMember(n => n.Tipo).SetElementName("tipo");
            cm.MapMember(n => n.Titulo).SetElementName("titulo");
            cm.MapMember(n => n.Mensagem).SetElementName("mensagem");
            cm.MapMember(n => n.DataEnvio).SetElementName("data_envio");
            cm.MapMember(n => n.Lida).SetElementName("lida");
            cm.MapMember(n => n.DataLeitura).SetElementName("data_leitura");
            cm.MapMember(n => n.DadosContexto).SetElementName("dados_contexto");
            cm.MapMember(n => n.Prioridade).SetElementName("prioridade");
            cm.MapMember(n => n.Origem).SetElementName("origem");
        });
    }
}