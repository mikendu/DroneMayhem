from munkres import Munkres, print_matrix
import random

"""

241	83	101	281	287	
209	290	235	161	135	
141	180	64	309	270	
153	90	251	166	182	
286	251	148	215	195	
"""

munkres = Munkres()
G = [
    [241,  83, 101, 281, 287],
    [209, 290, 235, 161, 135],
    [141, 180,  64, 309, 270],
    [153,  90, 251, 166, 182],
    [286, 251, 148, 215, 195],
]

# for i in range(0, len(G)):
#     G[i] = [float(x * 0.001) for x in G[i]]

print()
print()
print_matrix(G, msg="G:")
print()
print()

result = munkres.compute(G)
rows = list(sorted([item[0] for item in result]))
cols = list(sorted([item[1] for item in result]))

print("G result: ", result)
print("rows:", rows)
print("cols:", cols)


# count = 50
# matrix = []
#
#
# for i in range(0, count):
#     matrix.append([])
#
#     for j in range(0, count):
#         value = round(25.0 + (random.random() * 300.0))
#         matrix[i].append(value)
#
#
#
# print()
# print()
# print_matrix(matrix, msg="Matrix:")
# print()
# print()
#
#
# print()
# print()
# indexes = munkres.compute(matrix)
# rows_munk = list(sorted([item[0] for item in indexes]))
# cols_munk = list(sorted([item[1] for item in indexes]))
# print("munkres: ", indexes)
# print("rows munk:", rows_munk)
# print("cols munk:", cols_munk)
# print()
# print()


