﻿using Redes.Cliente;

using var cliente = new ClienteTcpBombParty();

await cliente.Iniciar("localhost", 9000);
