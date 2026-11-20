namespace InmueblesOperacion.Multitenancy
{
    public class TenantContext
    {
        public Guid TenantId { get; set; }
        public Guid UsuarioId { get; set; }
        public bool EsTitular { get; set; }
        public bool EsRevisor { get; set; }
    }
}
