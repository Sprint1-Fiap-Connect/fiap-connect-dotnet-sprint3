using FiapConnect.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FiapConnect.Infrastructure.Mongo.Mapeamentos;

// Mapeamento da Entity Conversa para a colecao "mensagens".
// Os campos da Entity (PascalCase) sao mapeados para os nomes reais
// dos documentos no Mongo (snake_case).
public static class ConversaMap
{
    public static void Registrar()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Conversa)))
        {
            BsonClassMap.RegisterClassMap<Conversa>(cm =>
            {
                cm.AutoMap();

                cm.MapIdMember(c => c.Id)
                  .SetSerializer(new StringSerializer(BsonType.ObjectId))
                  .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance);

                cm.MapMember(c => c.IdConversa).SetElementName("id_conversa");
                cm.MapMember(c => c.Participantes).SetElementName("participantes");
                cm.MapMember(c => c.NomesParticipantes).SetElementName("nomes_participantes");
                cm.MapMember(c => c.DataInicio).SetElementName("data_inicio");
                cm.MapMember(c => c.DataUltimaMensagem).SetElementName("data_ultima_mensagem");
                cm.MapMember(c => c.TotalMensagens).SetElementName("total_mensagens");
                cm.MapMember(c => c.MensagensNaoLidas).SetElementName("mensagens_nao_lidas");
                cm.MapMember(c => c.ContextoGrupoId).SetElementName("contexto_grupo_id");
                cm.MapMember(c => c.StatusConversa).SetElementName("status_conversa");
                cm.MapMember(c => c.Mensagens).SetElementName("mensagens");
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(MensagemItem)))
        {
            BsonClassMap.RegisterClassMap<MensagemItem>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(m => m.RemetenteRm).SetElementName("remetente_rm");
                cm.MapMember(m => m.Texto).SetElementName("texto");
                cm.MapMember(m => m.Timestamp).SetElementName("timestamp");
                cm.MapMember(m => m.Lida).SetElementName("lida");
            });
        }
    }
}