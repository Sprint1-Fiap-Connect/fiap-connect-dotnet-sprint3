using FiapConnect.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FiapConnect.Infrastructure.Mongo.Mapeamentos;

public static class AuditoriaMap
{
    public static void Registrar()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Auditoria))) return;

        BsonClassMap.RegisterClassMap<Auditoria>(cm =>
        {
            cm.AutoMap();

            cm.MapIdMember(a => a.Id)
              .SetSerializer(new StringSerializer(BsonType.ObjectId))
              .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance);

            cm.MapMember(a => a.TabelaAfetada).SetElementName("tabela_afetada");
            cm.MapMember(a => a.IdRegistro).SetElementName("id_registro");
            cm.MapMember(a => a.TipoOperacao).SetElementName("tipo_operacao");
            cm.MapMember(a => a.RmUsuario).SetElementName("rm_usuario");
            cm.MapMember(a => a.NomeUsuario).SetElementName("nome_usuario");
            cm.MapMember(a => a.DataOperacao).SetElementName("data_operacao");
            cm.MapMember(a => a.DadosAntes).SetElementName("dados_antes");
            cm.MapMember(a => a.DadosDepois).SetElementName("dados_depois");
            cm.MapMember(a => a.IpOrigem).SetElementName("ip_origem");
            cm.MapMember(a => a.SistemaOrigem).SetElementName("sistema_origem");
        });
    }
}