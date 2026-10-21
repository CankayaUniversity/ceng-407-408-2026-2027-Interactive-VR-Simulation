import type { SceneDefinition, Vector3Tuple } from './types'

// Digital Surgery, CC-BY-NC-4.0. Public Sketchfab annotations retrieved 2026-10-22.
// https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3
// Original English content and ordering are preserved, including source spelling.
interface SourceAnnotation {
  uid: string; title: string; description: string
  position: Vector3Tuple; eye: Vector3Tuple; target: Vector3Tuple
}

const annotations = [
  {
    "uid": "86c3b0f6a3ce47f5ac5de12f2d949256",
    "title": "Patient bed socket",
    "description": "The patients bed attaches to this sockets. Also provides communications with the bed to adjust the height, angle and tilt of the bed/headrest",
    "position": [
      0.005148481225094504,
      -0.06651238007191948,
      1.221689886149534
    ],
    "eye": [
      1.9579446712434643,
      -3.060704120173666,
      2.883488610908785
    ],
    "target": [
      -0.67973049816917,
      1.7123922454610259,
      0.05859976505854351
    ]
  },
  {
    "uid": "a19e55775e414144a3a95affdb6b9a1d",
    "title": "Anesthesiological tower",
    "description": "Contains all the equipment for the anesthesiologicist. Provides means for ventilation, injections and vital data monitoring.",
    "position": [
      -1.606890917391313,
      2.7266650743015712,
      1.9704723671977662
    ],
    "eye": [
      3.486518045265239,
      0.4570730204127931,
      3.3201186716711844
    ],
    "target": [
      -1.0729946757939244,
      3.095400530930605,
      1.8306051225623032
    ]
  },
  {
    "uid": "670762d9a1ea4ab4b90d99b1d8c67cfa",
    "title": "Main lock",
    "description": "Before entering the OR, patients are anesthezied, dressed in suitable clothing and have undergone some preperation. This is also the main lock which connects to the dressing rooms, gowning area and administrative operations.",
    "position": [
      -6.059324483836559,
      0.21091532176963887,
      1.3500620524178688
    ],
    "eye": [
      0.9741247019505987,
      -1.470865034629401,
      2.564583644841632
    ],
    "target": [
      -5.995589995438503,
      0.6253505781691512,
      1.9252949827553065
    ]
  },
  {
    "uid": "1029f524576f411595db01cad7d71c35",
    "title": "Room controls",
    "description": "This panel lets the staff adjust light levels, robotic arms, air conditioning and provides communication",
    "position": [
      -5.708091032532435,
      -2.4949414476401817,
      2.9535986160465573
    ],
    "eye": [
      -3.6547265659705808,
      -4.222968836909862,
      3.2004252669581446
    ],
    "target": [
      -5.927913364684443,
      -1.9649839252654404,
      2.82676802683993
    ]
  },
  {
    "uid": "487fc5aded7a4bf5b72687950b5f589b",
    "title": "Disinfactant supplies",
    "description": "Supplies for disenfacting the room, staff, patients and equipment. Also contains some commonly used items such as gloves, covering sheets ect.",
    "position": [
      -5.158293143782253,
      1.9376175055770988,
      1.6569419170895698
    ],
    "eye": [
      -3.369153221363642,
      0.5900984698252905,
      1.4776927208143773
    ],
    "target": [
      -5.2966192028882455,
      2.663649823621484,
      1.4297925630548254
    ]
  },
  {
    "uid": "6dc9a0daccdb4befb40e91f0ea721e6d",
    "title": "Main PC",
    "description": "PC used for displaying image data (X-Rays, CTs) onto the main display (6), administrative tasks, communication, ordering and protocolling.",
    "position": [
      -5.1407764021216416,
      4.389720678423876,
      2.7760885626229923
    ],
    "eye": [
      -2.4678517997022804,
      4.182573715748345,
      2.799360745121154
    ],
    "target": [
      -4.72652044473227,
      4.456776293421255,
      2.3922940683925216
    ]
  },
  {
    "uid": "31703e92eb7742b586d4258978529fd2",
    "title": "Main display",
    "description": "Large screen, used for showing patient data, such as the patient chart, X-Ray, MRI, CT, ultrasounds.",
    "position": [
      -1.2628599360643809,
      6.245748716319493,
      2.819720173012663
    ],
    "eye": [
      -3.261305288877637,
      4.597980984416786,
      3.521414708814567
    ],
    "target": [
      -1.0150971535170341,
      6.306166859272418,
      2.335466840859547
    ]
  },
  {
    "uid": "29478945ea22443f888e3b488599ecf3",
    "title": "Patient air conditioner",
    "description": "A patient can quickly cool out during surgery, so this air conditioner can be tucked under the patients blankets, to control body temperature.",
    "position": [
      -1.0584422869197754,
      5.559516612104243,
      0.8558158447542819
    ],
    "eye": [
      -2.5140712457361754,
      4.303662517888797,
      1.019951244088821
    ],
    "target": [
      -0.830524133700189,
      5.759678515693673,
      0.6667940137133055
    ]
  },
  {
    "uid": "aa3c58e8b6c640559b04c370d04ff235",
    "title": "Anesthesiological supplies",
    "description": "Supply table/cabinet, mainly used for storing anesthesiological supplies, such as narcotics, pain relief, syringes, and emergency equipment",
    "position": [
      0.7407382349759404,
      5.503475997610139,
      1.8476664499353035
    ],
    "eye": [
      3.1026267766296645,
      2.042334380693781,
      2.8684389640358345
    ],
    "target": [
      1.1398325735146648,
      6.125234380813031,
      1.4264794459450116
    ]
  },
  {
    "uid": "69b73467353f4b6eba1919152a91d0a4",
    "title": "Cauter & Suction tower",
    "description": "Provides devices to cauter (cut and burn) tissue, as well as suction devices, to remove body fluids.",
    "position": [
      3.802178884530005,
      1.125183903665251,
      2.4988415063965648
    ],
    "eye": [
      -1.8968791076603906,
      -0.6465148219627637,
      3.3494346872369367
    ],
    "target": [
      3.737804172785449,
      1.2774835396068598,
      2.2222387643478116
    ]
  }
] satisfies readonly SourceAnnotation[]

// Sketchfab annotations are Z-up. Match the GLB's Sketchfab_model -90deg X root.
const toYUp = ([x, y, z]: Vector3Tuple): Vector3Tuple => [x, z, -y]

export const hospitalStarterContent = {
  zones: annotations.map(item => ({
    id: 'sketchfab-view-' + item.uid,
    title: item.title,
    environmentId: 'charite-operating-room',
    camera: { position: toYUp(item.eye), target: toYUp(item.target), fovDegrees: 45 },
    calibration: 'reviewed' as const,
  })),
  hotspots: annotations.map(item => ({
    id: 'sketchfab-' + item.uid,
    title: item.title,
    description: item.description,
    position: toYUp(item.position),
    zoneId: 'sketchfab-view-' + item.uid,
  })),
} satisfies NonNullable<SceneDefinition['starterContent']>
