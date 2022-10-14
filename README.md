# LFA
*Ladestander Forbindelses Agent*

[![.NET](https://github.com/filip8600/LFA/actions/workflows/dotnet.yml/badge.svg)](https://github.com/filip8600/LFA/actions/workflows/dotnet.yml)

Muliggør forbindelse til adskillige elbils-ladestandere med WebSocket-forbindelser. Systemet er designet med et actor system "Proto.Actor". Løsningen er designet til at kommunikere med et Centralt Backend system (CS).
Se og hent CSSimulator her: https://github.com/LauritsHG/CSSimulator



Systemkrav:
- Docker (Herunder Kubectl)
- MiniKube (Kubernetes Cluster)
- Helm (Installering af images til Cluster)

Opsætning og kørsel:
- ```git clone https://github.com/filip8600/LFA```
- Byg og Push Image (Fra mappe med Dockerfile) ```docker build . -t filip8600/lfa:latest```
- Push til Image Repository (Fx Docker Hub) ```docker push filip8600/lfa:latest```
- Start Kluster ```./minicube.exe start```
- Release image til Cluster: ```helm install LFA chart-helm```
- Verificer at pods kører: ```Kubectl get pods```
- Åben port til service (for WS-trafik) ```minikube.exe service lfa --url```
- Send WebSocket-trafik med fx. Postman til "localhost:<port>/ws"

