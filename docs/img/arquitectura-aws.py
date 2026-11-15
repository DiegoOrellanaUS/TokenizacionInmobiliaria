"""Genera docs/img/arquitectura-aws.png.

Uso (requiere Graphviz):
    uv run --with diagrams python docs/img/arquitectura-aws.py
"""

from pathlib import Path

from diagrams import Cluster, Diagram, Edge
from diagrams.aws.analytics import Athena
from diagrams.aws.compute import Lambda
from diagrams.aws.database import RDS, Dynamodb, RDSPostgresqlInstance
from diagrams.aws.devtools import XRay
from diagrams.aws.integration import SQS, Eventbridge, EventbridgeScheduler, StepFunctions
from diagrams.aws.management import Cloudtrail, Cloudwatch
from diagrams.aws.ml import Bedrock
from diagrams.aws.mobile import Amplify
from diagrams.aws.network import APIGateway, NATGateway
from diagrams.aws.security import KMS, WAF, Cognito, Guardduty, SecretsManager
from diagrams.aws.storage import S3
from diagrams.onprem.client import Users
from diagrams.programming.flowchart import Database

OUT = Path(__file__).with_suffix("")

graph_attr = {"fontsize": "22", "pad": "0.6", "nodesep": "0.7", "ranksep": "1.1", "splines": "spline"}
cluster_attr = {"fontsize": "15"}
small = {"fontsize": "11"}

with Diagram(
    "Tokenización inmobiliaria: arquitectura en AWS (MVP)",
    filename=str(OUT),
    outformat="png",
    direction="LR",
    show=False,
    graph_attr=graph_attr,
    node_attr={"fontsize": "12"},
):
    usuarios = Users("Inversionistas,\nvendedores y\noperadores")

    with Cluster("Fuera de AWS", graph_attr=cluster_attr):
        pollar = Database("Pollar\n(rampa BOB a USDC)")
        kyc = Database("Proveedor de KYC")
        horizon = Database("Stellar Horizon")

    with Cluster("Cuenta de AWS", graph_attr=cluster_attr):

        with Cluster("Borde", graph_attr=cluster_attr):
            waf = WAF("WAF")
            with Cluster("Amplify Hosting", graph_attr=cluster_attr):
                web = Amplify("Next.js clientes")
                sell = Amplify("Next.js vendedores")
                admin = Amplify("Next.js administración\n(MFA + IP)")
            cognito = Cognito("Cognito\nlogin, MFA, grupos")
            api = APIGateway("API Gateway REST API\nautorizador Cognito")
            hooks = APIGateway("API Gateway REST API\nwebhooks")
            l_hook = Lambda("Receptor de webhooks\n(verifica firma)")

        with Cluster("AI Concierge (AgentCore)", graph_attr=cluster_attr):
            ac_runtime = Bedrock("AgentCore Runtime\n+ Guardrails")
            ac_gateway = Bedrock("AgentCore Gateway\nherramientas de lectura")
            ac_memory = Bedrock("AgentCore Memory")

        with Cluster("VPC, subredes privadas", graph_attr=cluster_attr):
            with Cluster("Servicios .NET 10 en Lambda", graph_attr=cluster_attr):
                l_admin = Lambda("Admin\ntenants, planes, KYC")
                l_inm = Lambda("Inmuebles y\nOperación")
                l_caja = Lambda("Caja y Pagos\nlibro mayor, rentas")
                l_com = Lambda("Comercio e\nInversión")
                l_rep = Lambda("Reportes")
            l_tok = Lambda("Tokenization\nsin ruta pública")
            proxy = RDS("RDS Proxy\nauth IAM")
            rds = RDSPostgresqlInstance("RDS PostgreSQL\nun esquema por servicio")
            nat = NATGateway("NAT Gateway")

        with Cluster("Orquestación y eventos", graph_attr=cluster_attr):
            sfn = StepFunctions("Step Functions\nsaga de compra")
            bus = Eventbridge("EventBridge\nrecibe el outbox de cada servicio\n(líneas naranjas)")
            colas = SQS("Colas SQS + DLQ")
            fifo = SQS("SQS FIFO\nenvíos a Stellar")
            sched = EventbridgeScheduler("Scheduler\ncierres, conciliación")

        with Cluster("Datos administrados", graph_attr=cluster_attr):
            ddb_rep = Dynamodb("DynamoDB\nmodelos de lectura\nde Reportes")
            ddb_idem = Dynamodb("DynamoDB\nidempotencia")
            s3 = S3("S3 documentos\nSSE-KMS")
            athena = Athena("Athena\nanálisis ad hoc")

        with Cluster("Seguridad y operación (toda la cuenta)", graph_attr=cluster_attr):
            kms = KMS("KMS\nllaves Ed25519")
            secrets = SecretsManager("Secrets Manager")
            guard = Guardduty("GuardDuty\n+ malware S3")
            cw = Cloudwatch("CloudWatch")
            xray = XRay("X-Ray")
            trail = Cloudtrail("CloudTrail")

    # Usuarios y borde
    usuarios >> waf >> [web, sell, admin]
    [web, sell, admin] >> Edge(label="login", **small) >> cognito
    [web, sell, admin] >> Edge(label="JWT", **small) >> api
    usuarios >> Edge(label="chat", **small) >> ac_runtime

    # Asistente
    ac_runtime >> ac_memory
    ac_runtime >> ac_gateway >> Edge(label="token del usuario", **small) >> api

    # API hacia servicios
    api >> [l_admin, l_inm, l_caja, l_com, l_rep]

    # Saga de compra
    l_com >> Edge(label="inicia", **small) >> sfn
    sfn >> l_caja
    sfn >> Edge(label="waitForTaskToken", **small) >> fifo >> l_tok

    # Eventos
    for svc in [l_admin, l_inm, l_caja, l_com, l_tok]:
        svc >> Edge(style="dashed", color="darkorange") >> bus
    bus >> colas >> [l_rep, l_caja, l_admin]
    sched >> Edge(style="dashed") >> [l_inm, l_caja, l_tok]

    # Datos
    [l_admin, l_inm, l_caja, l_com, l_tok] >> proxy >> rds
    l_rep >> ddb_rep
    ddb_rep >> Edge(label="exportación", style="dashed", **small) >> s3 >> athena
    [l_caja, l_com] >> Edge(style="dotted") >> ddb_idem
    l_inm >> s3

    # Stellar y externos
    l_tok >> Edge(label="kms:Sign", color="firebrick", **small) >> kms
    [l_tok, l_caja, l_admin] >> nat >> [pollar, kyc, horizon]
    [pollar, kyc] >> Edge(label="webhooks", **small) >> hooks >> l_hook >> colas

    # Seguridad y operación (aplica a toda la cuenta; se muestran solo algunas relaciones)
    gris = {"style": "dotted", "color": "gray"}
    proxy >> Edge(**gris) >> secrets
    s3 >> Edge(**gris) >> guard
    l_caja >> Edge(**gris) >> [cw, xray]
    api >> Edge(**gris) >> trail
