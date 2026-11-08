namespace Primora.Domain.Entities;

public sealed class IndicadoresUnidade
{
    private IndicadoresUnidade() { }

    public IndicadoresUnidade(Guid versaoId, string unidadeId, int carteiraTotalClientes, decimal receitaCarteira90d, int eventosEngajamento7d, int usuariosAtivos7d, int funilGerada90d, int funilVista90d, int funilSucesso90d, int funilFalha90d, int saudavel, int atencao, int risco, int critico, int riscoAbsoluto)
    {
        VersaoId = Validacao.Identificador(versaoId, nameof(versaoId));
        UnidadeId = Validacao.Texto(unidadeId, nameof(unidadeId));
        CarteiraTotalClientes = Validacao.NaoNegativo(carteiraTotalClientes, nameof(carteiraTotalClientes));
        ReceitaCarteira90d = Validacao.NaoNegativo(receitaCarteira90d, nameof(receitaCarteira90d));
        EventosEngajamento7d = Validacao.NaoNegativo(eventosEngajamento7d, nameof(eventosEngajamento7d));
        UsuariosAtivos7d = Validacao.NaoNegativo(usuariosAtivos7d, nameof(usuariosAtivos7d));
        FunilGerada90d = Validacao.NaoNegativo(funilGerada90d, nameof(funilGerada90d));
        FunilVista90d = Validacao.NaoNegativo(funilVista90d, nameof(funilVista90d));
        FunilSucesso90d = Validacao.NaoNegativo(funilSucesso90d, nameof(funilSucesso90d));
        FunilFalha90d = Validacao.NaoNegativo(funilFalha90d, nameof(funilFalha90d));
        Saudavel = Validacao.NaoNegativo(saudavel, nameof(saudavel));
        Atencao = Validacao.NaoNegativo(atencao, nameof(atencao));
        Risco = Validacao.NaoNegativo(risco, nameof(risco));
        Critico = Validacao.NaoNegativo(critico, nameof(critico));
        RiscoAbsoluto = Validacao.NaoNegativo(riscoAbsoluto, nameof(riscoAbsoluto));
        if ((long)Saudavel + Atencao + Risco + Critico != CarteiraTotalClientes)
            throw new ArgumentException("A distribuição de saúde deve corresponder à carteira.", nameof(carteiraTotalClientes));
        if ((long)Risco + Critico != RiscoAbsoluto)
            throw new ArgumentException("O risco absoluto deve corresponder aos clientes em risco ou críticos.", nameof(riscoAbsoluto));
    }

    public Guid VersaoId { get; private set; }
    public string UnidadeId { get; private set; } = string.Empty;
    public int CarteiraTotalClientes { get; private set; }
    public decimal ReceitaCarteira90d { get; private set; }
    public int EventosEngajamento7d { get; private set; }
    public int UsuariosAtivos7d { get; private set; }
    public int FunilGerada90d { get; private set; }
    public int FunilVista90d { get; private set; }
    public int FunilSucesso90d { get; private set; }
    public int FunilFalha90d { get; private set; }
    public int Saudavel { get; private set; }
    public int Atencao { get; private set; }
    public int Risco { get; private set; }
    public int Critico { get; private set; }
    public int RiscoAbsoluto { get; private set; }

    public Unidade Unidade { get; private set; } = null!;
}
