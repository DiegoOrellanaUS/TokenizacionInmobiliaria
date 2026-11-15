"""Diagrama AWS de la sección 2.1 de docs/arquitectura-del-sistema.md.

Regenerar desde la raíz del repositorio (requiere Graphviz, incluido en devbox):

    devbox run -- uvx --with diagrams python docs/diagramas/arquitectura-aws.py
"""

from pathlib import Path

from diagrams import Cluster, Diagram, Edge
from diagrams.aws.compute import Lambda
from diagrams.aws.database import RDS, Dynamodb, RDSPostgresqlInstance
from diagrams.aws.general import Users
from diagrams.aws.integration import SQS, Eventbridge, EventbridgeScheduler, StepFunctions
from diagrams.aws.management import Cloudtrail, Cloudwatch
from diagrams.aws.ml import Bedrock
from diagrams.aws.mobile import Amplify
from diagrams.aws.network import APIGateway, NATGateway
from diagrams.aws.security import KMS, Cognito, Guardduty, SecretsManager, WAF
from diagrams.aws.storage import S3
from diagrams.aws.devtools import XRay
from diagrams.onprem.client import Client
from diagrams.programming.flowchart import Database as External

OUT = Path(__file__).with_suffix("")

graph_attr = {
    "fontsize": "20",
    "pad": "0.5",
    "nodesep": "0.5",
    "ranksep": "1.2",
    "compound": "true",
    "splines": "spline",
}

# Las aristas que entran o salen del grupo de Lambdas se dibujan una sola vez,
# hacia o desde el borde del cluster, en lugar de una por función.
SVC_CLUSTER = "cluster_Subredes privadas: Lambda .NET"


def to_svc(**kw):
    return Edge(lhead=SVC_CLUSTER, **kw)


def from_svc(**kw):
    return Edge(ltail=SVC_CLUSTER, **kw)

with Diagram(
    "Tokenización Inmobiliaria - AWS",
    filename=str(OUT),
    outformat="png",
    show=False,
    direction="LR",
    graph_attr=graph_attr,
):
    users = Users("Inversionistas,\nvendedores y operadores")
    pollar = Client("Pollar /\nproveedor de KYC")
    horizon = External("Stellar Horizon")

    with Cluster("AWS Cloud"):
        with Cluster("Amplify Hosting + WAF"):
            fe_waf = WAF("WAF")
            web = Amplify("Next.js clientes")
            sell = Amplify("Next.js vendedores")
            admin = Amplify("Next.js administración")
            frontends = [web, sell, admin]

        cognito = Cognito("Cognito\nlogin, MFA, roles")

        with Cluster("API"):
            api_waf = WAF("WAF")
            api = APIGateway("REST API")

        with Cluster("API webhooks"):
            hooks_waf = WAF("WAF")
            hooks = APIGateway("REST API\nwebhooks")

        with Cluster("AI Concierge"):
            ac_runtime = Bedrock("AgentCore Runtime\n+ Guardrails")
            ac_memory = Bedrock("AgentCore Memory")
            ac_gateway = Bedrock("AgentCore Gateway\nsolo lectura")

        with Cluster("Eventos"):
            bus = Eventbridge("EventBridge")
            queues = SQS("SQS + DLQ")
            fifo = SQS("SQS FIFO\nenvíos a Stellar")
            scheduler = EventbridgeScheduler("Scheduler")

        saga = StepFunctions("Step Functions\nsaga de compra")

        with Cluster("VPC"):
            with Cluster("Subredes privadas: Lambda .NET"):
                l_adm = Lambda("Admin")
                l_inm = Lambda("Inmuebles y\nOperación")
                l_caj = Lambda("Caja y Pagos")
                l_com = Lambda("Comercio e\nInversión")
                l_rep = Lambda("Reportes")
                l_tok = Lambda("Tokenization\nsin ruta pública")

            with Cluster("Datos"):
                proxy = RDS("RDS Proxy")
                db = RDSPostgresqlInstance("RDS PostgreSQL\nun esquema por servicio")

            nat = NATGateway("NAT Gateway")

        docs = S3("S3 documentos")
        malware = Guardduty("GuardDuty\nescaneo de malware")
        ddb = Dynamodb("DynamoDB\nidempotencia")
        kms = KMS("KMS\nllaves Ed25519")
        secrets = SecretsManager("Secrets Manager")

        with Cluster("Observabilidad y auditoría"):
            Cloudwatch("CloudWatch")
            XRay("X-Ray")
            Cloudtrail("CloudTrail")

    # Entrada de usuarios
    users >> fe_waf >> frontends
    web >> Edge(label="login") >> cognito
    web >> Edge(label="JWT") >> api_waf >> api
    sell >> cognito
    sell >> api_waf
    admin >> cognito
    admin >> api_waf
    api >> to_svc(label="autorizador Cognito") >> l_inm

    # Asistente de IA
    users >> Edge(label="chat") >> ac_runtime
    ac_runtime >> ac_memory
    ac_runtime >> ac_gateway >> Edge(label="token del usuario") >> api

    # Saga de compra
    l_com >> saga
    saga >> l_caj
    saga >> fifo >> l_tok

    # Eventos (Tokenization está dentro del cluster, así que queda incluido)
    l_inm >> from_svc(label="outbox", style="dashed") >> bus
    bus >> queues >> to_svc() >> l_inm
    scheduler >> to_svc() >> l_inm

    # Datos
    l_inm >> from_svc() >> proxy
    proxy >> db
    proxy >> secrets
    l_inm >> from_svc() >> docs
    docs >> malware
    l_inm >> from_svc() >> ddb
    l_tok >> Edge(label="firma") >> kms

    # Salida a internet
    l_inm >> from_svc() >> nat
    nat >> pollar
    nat >> horizon

    # Webhooks
    pollar >> Edge(label="webhooks") >> hooks_waf >> hooks >> queues
