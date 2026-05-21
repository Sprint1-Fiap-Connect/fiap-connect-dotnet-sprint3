using FiapConnect.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FiapConnect.Infrastructure.Mongo.Mapeamentos;

public static class HistoricoBuscaMap
{
    public static void Registrar()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(HistoricoBusca)))
        {
            BsonClassMap.RegisterClassMap<HistoricoBusca>(cm =>
            {
                cm.AutoMap();

                cm.MapIdMember(h => h.Id)
                  .SetSerializer(new StringSerializer(BsonType.ObjectId))
                  .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance);

                cm.MapMember(h => h.RmAluno).SetElementName("rm_aluno");
                cm.MapMember(h => h.NomeAluno).SetElementName("nome_aluno");
                cm.MapMember(h => h.Timestamp).SetElementName("timestamp");
                cm.MapMember(h => h.FiltroDisciplina).SetElementName("filtro_disciplina");
                cm.MapMember(h => h.EdicaoChallenge).SetElementName("edicao_challenge");
                cm.MapMember(h => h.HabilidadesAluno).SetElementName("habilidades_aluno");
                cm.MapMember(h => h.TotalGruposRetornados).SetElementName("total_grupos_retornados");
                cm.MapMember(h => h.GruposRetornados).SetElementName("grupos_retornados");
                cm.MapMember(h => h.GrupoClicadoId).SetElementName("grupo_clicado_id");
                cm.MapMember(h => h.SolicitacaoEnviada).SetElementName("solicitacao_enviada");
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(GrupoRetornado)))
        {
            BsonClassMap.RegisterClassMap<GrupoRetornado>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(g => g.IdGrupo).SetElementName("id_grupo");
                cm.MapMember(g => g.NomeGrupo).SetElementName("nome_grupo");
                cm.MapMember(g => g.Percentual).SetElementName("percentual");
                cm.MapMember(g => g.ClassificacaoIa).SetElementName("classificacao_ia");
            });
        }
    }
}