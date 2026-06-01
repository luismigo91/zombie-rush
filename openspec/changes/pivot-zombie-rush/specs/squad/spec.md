## ADDED Requirements

### Requirement: Movimiento lateral del escuadrón

El jugador SHALL controlar la posición del escuadrón únicamente en el eje horizontal (X) mediante arrastre del dedo o del ratón. El escuadrón MUST permanecer dentro de los límites laterales visibles de la pantalla.

#### Scenario: Arrastrar para mover

- **WHEN** el jugador mantiene pulsado y arrastra horizontalmente
- **THEN** el escuadrón se desplaza en X siguiendo el dedo, sin moverse en Y por acción del jugador

#### Scenario: Límites de pantalla

- **WHEN** el jugador arrastra más allá del borde izquierdo o derecho
- **THEN** el escuadrón se detiene en el borde y no sale de la zona jugable

### Requirement: Formación de multitud

Las unidades del escuadrón SHALL agruparse en una formación de multitud (blob) cuyo ancho aumenta con el número de unidades. La formación MUST recolocar a las unidades automáticamente cuando el recuento sube o baja.

#### Scenario: El ancho crece con las unidades

- **WHEN** el recuento de unidades aumenta
- **THEN** el blob ocupa más anchura horizontal, ampliando la cobertura de fuego

#### Scenario: Recolocación al perder unidades

- **WHEN** el recuento de unidades disminuye
- **THEN** la formación se recompone manteniéndose como un grupo coherente

### Requirement: Disparo recto automático

Cada unidad del escuadrón SHALL disparar de forma automática en línea recta hacia arriba (eje +Y), sin auto-apuntado ni intervención del jugador sobre la puntería.

#### Scenario: Fuego continuo

- **WHEN** hay al menos una unidad viva y el nivel está en curso
- **THEN** las unidades emiten proyectiles rectos hacia arriba a su cadencia

#### Scenario: La cobertura depende de la posición y el ancho

- **WHEN** el jugador alinea el blob con una columna de enemigos
- **THEN** los proyectiles de esa franja impactan en esa columna, y las columnas no cubiertas por el ancho del blob no reciben fuego

### Requirement: Recuento de unidades como recurso central

El número de unidades SHALL ser el recurso principal de la partida: crece con elementos del recorrido y decrece por contacto con enemigos. El sistema MUST exponer el recuento actual a la interfaz.

#### Scenario: Aumentar unidades

- **WHEN** el escuadrón obtiene unidades (gate o rescate)
- **THEN** el recuento sube y se añaden unidades a la formación

#### Scenario: Recuento visible

- **WHEN** el nivel está en curso
- **THEN** la interfaz muestra el número actual de unidades

### Requirement: Reinicio del escuadrón por nivel

Al comenzar cada nivel, el escuadrón SHALL reiniciarse al punto de partida definido por la meta-progresión, descartando el tamaño alcanzado en niveles anteriores.

#### Scenario: Empezar un nivel

- **WHEN** comienza un nivel nuevo
- **THEN** el escuadrón arranca con el tamaño y arma base del punto de partida, no con el tamaño final del nivel previo
