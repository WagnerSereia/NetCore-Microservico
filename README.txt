# FIAP-Microservico_GeekBurgerProduction
Projeto de implementação do microservico de producao.

-Api Mockada para nova ordem (NewOrder) em topico no serviceBus
-Api para publicar uma nova ordem no topico OrderChanged
-Api para publicar uma alteração em uma ordem (finished order) no topico OrderChanged
-Api para alterar aleatoriamente a area da producao publicando mensagem no topico ProductionAreaChanged
-Api para mostrar o uso do Polly acessando via httpCliente identificada como ChamadaApiTestePolly na controller OrdersController (url = /api/orders/ChamadaApiTestePolly)