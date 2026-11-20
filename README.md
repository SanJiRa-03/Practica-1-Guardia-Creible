# Guardia creíble

Un guardia de seguridad para Unity que ve, oye, sospecha, investiga, persigue y vuelve a patrullar. Su comportamiento puede estar controlado por una **máquina de estados (FSM)** o por un **árbol de comportamiento (BT)**, intercambiables desde el Inspector sin tocar la percepción ni el movimiento.

Práctica 1 de Inteligencia Artificial (IA3), Grado en Diseño y Desarrollo de Videojuegos, UDIT. Curso 2026/2027.

**Autores:** Sandra Jiménez Ramiro y Alfredo Crespo Salvá.

## Contenido

1. [Qué hace](#qué-hace)
2. [Requisitos](#requisitos)
3. [Cómo ejecutarlo](#cómo-ejecutarlo)
4. [Controles](#controles)
5. [Cómo está organizado](#cómo-está-organizado)
6. [Percepción](#percepción)
7. [Los dos cerebros](#los-dos-cerebros)
8. [Parámetros ajustables](#parámetros-ajustables)
9. [Apartados opcionales](#apartados-opcionales)
10. [Reparto del trabajo](#reparto-del-trabajo)
11. [Créditos](#créditos)

## Qué hace

La escena es un suelo de unos 40 × 40 m con pasillos, una habitación abierta con escondites y muros que bloquean la vista. Hay dos guardias con rutas de waypoints distintas: uno usa la FSM y el otro el árbol, para poder compararlos a la vez.

El guardia sigue este ciclo:

| Situación | Comportamiento |
|---|---|
| No sabe nada | Patrulla sus waypoints |
| Sospecha algo | Investiga: se acerca al lugar sin comprometerse |
| Ve al jugador | Persigue |
| Lo pierde de vista | Busca alrededor de la última posición conocida |
| No encuentra nada | Vuelve a patrullar |

## Requisitos

- Unity **6000.6.1f1**
- Paquete **AI Navigation 2.0.16** (se instala desde el Package Manager)
- Starter Assets (controlador del jugador, incluido en el proyecto)

## Cómo ejecutarlo

1. Clona el repositorio:
   ```
   git clone https://github.com/SanJiRa-03/Practica-1-Guardia-Creible.git
   ```
2. Abre la carpeta con Unity Hub usando la versión 6000.6.1f1. La carpeta `Library/` no se incluye; Unity la genera al abrir el proyecto.
3. Abre la escena principal y pulsa Play.
4. Si los guardias no se mueven, comprueba que el NavMesh está generado (componente NavMesh Surface, botón Bake).

Para cambiar el cerebro de un guardia, selecciónalo y cambia el desplegable **Cerebro** del componente `GuardiaAgente` (Máquina de Estados o Árbol de Comportamiento).

## Controles

| Tecla | Acción |
|---|---|
| W, A, S, D | Mover al jugador |
| Ratón | Cámara |
| G | Genera un ruido 8 m por delante del jugador (sirve para probar la audición) |

Los pasos del jugador también generan ruido, más fuerte al correr.

## Cómo está organizado

Los datos van siempre en un solo sentido y cada capa tiene una única responsabilidad:

```
Percepción  →  Blackboard  →  Decisión (FSM o BT)  →  Movimiento
(sensores)     (memoria)      (lee y escribe          (NavMeshAgent)
                               intenciones)
```

- **Sensores** (`SensorVision`, `SensorAudicion`): miran el mundo y escriben en el blackboard. No toman decisiones.
- **Blackboard** (`ClavesBB`): memoria compartida: nivel de sospecha, última posición, ruido pendiente, destino, etc.
- **Decisor** (`DecisorFSM` o `DecisorBT`): lee el blackboard y escribe el destino. No mira el mundo ni mueve al guardia.
- **Locomoción** (`LocomocionNavMesh`): lee el destino y mueve al guardia. Es la única clase que usa el `NavMeshAgent`.

Carpetas principales, dentro de `Assets/GuardiaBase/Scripts/`:

| Carpeta | Contenido |
|---|---|
| `Nucleo` | Claves del blackboard, contratos (`ISensor`, `IDecisor`) y `GuardiaAgente` |
| `Percepcion` | `NivelSospecha`, `SensorVision`, `SensorAudicion`, `RuidoMundo`, `EmisorPasos`, `EmisorRuidoTecla` |
| `Movimiento` | `LocomocionNavMesh`, `RutaPatrulla` |
| `FSM` | `StateMachine`, `DecisorFSM` y los estados `EstadoPatrullar`, `EstadoInvestigar`, `EstadoPerseguir`, `EstadoBuscar` |
| `BT` | Runtime del árbol y `DecisorBT` |
| `Depuracion` | `GuardiaGizmos` (cono de visión, barra de sospecha, última posición) |

## Percepción

El guardia no "ve o no ve": tiene un **medidor de sospecha** `s` entre 0 y 1 que sube cuando percibe al jugador y baja poco a poco cuando no.

**Visión.** Alcance de 15 m y cono de 70°. Se lanzan 3 rayos al jugador para tener en cuenta que un muro lo tape en parte. La evidencia `e` depende de la distancia, del ángulo respecto al centro del cono y de cuántos rayos llegan:

```
e = f_distancia · (f_ángulo + f_rayos) / 2
s(t+Δt) = clamp( s + ( a·e·v − d·(1−v) ) · Δt )
```

donde `v` vale 1 si hay percepción y 0 si no, `a` es la velocidad de subida y `d` la de olvido.

**Audición.** Los pasos y la tecla G emiten un ruido en el mundo. El volumen percibido es `volumen × (1 − distancia / 15)`. Se oye a partir de 0,1 y se considera ruido fuerte a partir de 0,5. El oído por sí solo nunca lleva al guardia a Alerta (hay un tope de sospecha).

**Estados del medidor.** Tranquilo, Sospecha y Alerta, con histéresis para que el guardia no parpadee entre estados cuando la sospecha está cerca de un umbral.

## Los dos cerebros

**Máquina de estados.** Un estado por clase (`Patrullar`, `Investigar`, `Perseguir`, `Buscar`). Las transiciones leen el medidor del blackboard. En `Investigar` el guardia visita hasta 3 puntos candidatos cercanos a la última posición, elegidos por cercanía y escondites; después busca un momento y vuelve a patrullar.

**Árbol de comportamiento.** Raíz `ActiveSelector` que reevalúa las ramas por prioridad en cada ciclo (10 Hz): Alerta, Sospecha, Ruido, Última posición y, al final, Patrullar. La rama de última posición hace `IrA` y `MirarAlrededor` (12 s, radio de 4 m).

**Comparativa con datos propios** (solo código propio, sin contar el runtime del árbol ni la clase base `StateMachine`):

| | FSM | Árbol |
|---|---|---|
| Clases propias | 5 | 3 |
| Líneas de código | 411 | 134 |
| Estructura | 4 estados y 10 transiciones reales | 16 nodos (5 de control y 11 hojas), sin transiciones |

El análisis completo está en la memoria (apartado E).

## Parámetros ajustables

Se editan en el Inspector. Los valores finales, tras el ajuste descrito en la memoria, son:

| Parámetro | Valor | Efecto |
|---|---|---|
| Velocidad Subida (`a`) | 0,8 | Rapidez con que sube la sospecha |
| Velocidad Bajada (`d`) | 0,08 | Rapidez con que se olvida |
| Umbral Sospecha | 0,35 | A partir de aquí el guardia investiga |
| Umbral Alerta | 0,95 | A partir de aquí el guardia persigue |
| Histéresis | 0,1 | Margen para volver a un estado inferior |
| Salto Ruido | 0,4 | Subida puntual de sospecha por un ruido fuerte |
| Tope Sospecha | 0,6 | Máximo al que puede llevar el oído solo |

Los valores del Inspector anulan los que se escriben en el código, y los cambios hechos en modo Play se pierden al salir.

## Apartados opcionales

- **Audición**: sensor de oído y eventos de ruido del mundo.
- **R+**: reacción a un ruido fuerte en el árbol (rama `SiRuido`).
- **Blackboard compartido**: alerta de grupo con latencia entre guardias.
- **Waypoints inteligentes**: puntos con memoria de visita e interés que condicionan la ruta.
- **Consulta de posición** de los waypoints.

## Reparto del trabajo

| Autor | Partes |
|---|---|
| Sandra Jiménez Ramiro | Escena, percepción (A), árbol de comportamiento (C), intercambiabilidad (D), audición, R+, ajuste del medidor (E.3), memoria y vídeo |
| Alfredo Crespo Salvá | Máquina de estados (B), comparativa (E.1), análisis de R+ (E.2), blackboard compartido, waypoints inteligentes, consulta de posición |

El historial de commits refleja la contribución de cada uno.

## Documentación y vídeo

- Memoria: `https://drive.google.com/file/d/1EAEGz9Jymp99FEURnRSNmX07_Ff8Eyyq/view?usp=sharing`
- Vídeo de demostración (2 a 4 minutos): `https://drive.google.com/file/d/13-w2U6_evEKbr5aOwfaxUdpcxYq1LZn5/view?usp=drive_link`

## Créditos

- Proyecto base, runtime del árbol de comportamiento, `StateMachine` y ejemplo `EstadoPatrullar`: Dr. Luis Peña Sánchez (UDIT).
- Controlador del jugador: Starter Assets de Unity.
- Se ha usado asistente de IA como apoyo (Claude) para la redacción de este README.md.
