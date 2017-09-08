import random
import hashlib

from node import *
from routing import *

rid1 = hashlib.sha1(str(random.getrandbits(255)).encode('utf-8')).digest()
rid2 = hashlib.sha1(str(random.getrandbits(255)).encode('utf-8')).digest()

hostNode = Node(rid1)
otherNode = Node(rid2)
# nodeHeap = NodeHeap(hostNode, 20)
router = RoutingTable(20, hostNode)
router.addContact(otherNode)

