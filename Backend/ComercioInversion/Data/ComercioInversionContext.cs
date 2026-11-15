using ComercioInversion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Data
{
    public class ComercioInversionContext(DbContextOptions<ComercioInversionContext> options) : DbContext(options)
    {
        public DbSet<Participante> Participante { get; set; } = default!;
        public DbSet<KycCaso> KycCaso { get; set; } = default!;
        public DbSet<KycDecision> KycDecision { get; set; } = default!;
        public DbSet<KycEvidencia> KycEvidencia { get; set; } = default!;
        public DbSet<Instrumento> Instrumento { get; set; } = default!;
        public DbSet<InstrumentoInmueble> InstrumentoInmueble { get; set; } = default!;
        public DbSet<Oferta> Oferta { get; set; } = default!;
        public DbSet<OfertaVersion> OfertaVersion { get; set; } = default!;
        public DbSet<OfertaDocumento> OfertaDocumento { get; set; } = default!;
        public DbSet<Orden> Orden { get; set; } = default!;
        public DbSet<VinculoReserva> VinculoReserva { get; set; } = default!;
        public DbSet<Ejecucion> Ejecucion { get; set; } = default!;
        public DbSet<Liquidacion> Liquidacion { get; set; } = default!;
        public DbSet<Compra> Compra { get; set; } = default!;
        public DbSet<CompraVersion> CompraVersion { get; set; } = default!;
        public DbSet<EventoCompra> EventoCompra { get; set; } = default!;
        public DbSet<Distribucion> Distribucion { get; set; } = default!;
        public DbSet<DistribucionCierre> DistribucionCierre { get; set; } = default!;
        public DbSet<DistribucionLinea> DistribucionLinea { get; set; } = default!;
        public DbSet<ValuacionInstrumento> ValuacionInstrumento { get; set; } = default!;
        public DbSet<ValuacionVersion> ValuacionVersion { get; set; } = default!;
        public DbSet<ValuacionComponente> ValuacionComponente { get; set; } = default!;
        public DbSet<ValuacionAvaluo> ValuacionAvaluo { get; set; } = default!;
        public DbSet<CotizacionCompra> CotizacionCompra { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KycCaso>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_kyc_caso_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Participante)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_kyc_caso_1");
            });

            modelBuilder.Entity<KycDecision>(entidad =>
            {
                entidad.HasOne(fila => fila.Caso)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_kyc_decision_2");
            });

            modelBuilder.Entity<KycEvidencia>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_kyc_evidencia_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Caso)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_kyc_evidencia_3");
            });

            modelBuilder.Entity<Instrumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_instrumento_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Emisor)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instrumento_4");
            });

            modelBuilder.Entity<InstrumentoInmueble>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_instrumento_inmueble_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.instrumento_id, fila.inmueble_ref })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_instrumento_inmueble_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Instrumento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instrumento_inmueble_5");
            });

            modelBuilder.Entity<Oferta>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_oferta_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Instrumento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_oferta_6");

                entidad.HasOne(fila => fila.Oferente)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_oferta_7");
            });

            modelBuilder.Entity<OfertaVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_oferta_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Oferta)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_oferta_version_8");
            });

            modelBuilder.Entity<OfertaDocumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_oferta_documento_1", "oferta_version > 0");
                    tabla.HasCheckConstraint("ck_oferta_documento_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.OfertaVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_oferta_documento_9");
            });

            modelBuilder.Entity<Orden>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_orden_1", "oferta_version > 0");
                    tabla.HasCheckConstraint("ck_orden_2", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("orden_idempotencia_key");

                entidad.HasOne(fila => fila.Participante)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_orden_10");

                entidad.HasOne(fila => fila.OfertaVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_orden_11");
            });

            modelBuilder.Entity<VinculoReserva>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_vinculo_reserva_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Orden)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_vinculo_reserva_12");
            });

            modelBuilder.Entity<Ejecucion>(entidad =>
            {
                entidad.HasOne(fila => fila.OrdenCompra)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_ejecucion_13");

                entidad.HasOne(fila => fila.OrdenVenta)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_ejecucion_14");
            });

            modelBuilder.Entity<Liquidacion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_liquidacion_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Ejecucion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_liquidacion_15");
            });

            modelBuilder.Entity<Compra>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.ejecucion_id)
                    .IsUnique()
                    .HasDatabaseName("compra_ejecucion_id_key");

                entidad.HasOne(fila => fila.Ejecucion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_16");
            });

            modelBuilder.Entity<CompraVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_version_1", "version > 0");
                    tabla.HasCheckConstraint("ck_compra_version_2", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Compra)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_version_17");
            });

            modelBuilder.Entity<EventoCompra>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_evento_compra_1", "compra_version > 0");
                });

                entidad.HasIndex(fila => new { fila.compra_id, fila.compra_version })
                    .IsUnique()
                    .HasDatabaseName("evento_compra_compra_id_compra_version_key");

                entidad.HasOne(fila => fila.CompraVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_evento_compra_18");
            });

            modelBuilder.Entity<Distribucion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_distribucion_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Instrumento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_distribucion_19");
            });

            modelBuilder.Entity<DistribucionCierre>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_distribucion_cierre_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Distribucion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_distribucion_cierre_20");
            });

            modelBuilder.Entity<DistribucionLinea>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_distribucion_linea_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Distribucion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_distribucion_linea_21");

                entidad.HasOne(fila => fila.Participante)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_distribucion_linea_22");
            });

            modelBuilder.Entity<ValuacionInstrumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_valuacion_instrumento_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Instrumento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valuacion_instrumento_23");
            });

            modelBuilder.Entity<ValuacionVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_valuacion_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Valuacion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valuacion_version_24");
            });

            modelBuilder.Entity<ValuacionComponente>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_valuacion_componente_1", "version > 0");
                });

                entidad.HasOne(fila => fila.ValuacionVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valuacion_componente_25");
            });

            modelBuilder.Entity<ValuacionAvaluo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_valuacion_avaluo_1", "version > 0");
                    tabla.HasCheckConstraint("ck_valuacion_avaluo_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.ValuacionVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valuacion_avaluo_26");
            });

            modelBuilder.Entity<CotizacionCompra>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cotizacion_compra_1", "vence_en > reservada_en");
                    tabla.HasCheckConstraint("ck_cotizacion_compra_2", "cantidad_base > 0");
                    tabla.HasCheckConstraint("ck_cotizacion_compra_3", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Orden)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cotizacion_compra_27");
            });

            modelBuilder.Entity<Participante>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_participante_1", "btrim(estado) <> ''");
                });
            });
        }
    }
}
